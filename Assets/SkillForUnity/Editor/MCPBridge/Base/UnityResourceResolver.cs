using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// GameObject専用のリソース解決実装。
    /// </summary>
    public class GameObjectResolver : IGameObjectResolver
    {
        public GameObject Resolve(string identifier)
        {
            var result = TryResolve(identifier);
            if (result == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{identifier}'");
            }
            return result;
        }
        
        public GameObject TryResolve(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }
            
            // 階層パスで検索
            return ResolveByHierarchyPath(identifier);
        }
        
        public bool Exists(string identifier)
        {
            return TryResolve(identifier) != null;
        }
        
        public IEnumerable<GameObject> ResolveMany(params string[] identifiers)
        {
            return identifiers
                .Select(id => TryResolve(id))
                .Where(go => go != null);
        }
        
        public GameObject ResolveByHierarchyPath(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
            {
                return null;
            }
            
            // パスを "/" で分割
            var parts = hierarchyPath.Split('/');
            GameObject current = null;
            
            // ルートオブジェクトを検索
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            current = rootObjects.FirstOrDefault(go => go.name == parts[0]);
            
            if (current == null)
            {
                return null;
            }
            
            // 子オブジェクトを順に検索
            for (int i = 1; i < parts.Length; i++)
            {
                var childTransform = current.transform.Find(parts[i]);
                if (childTransform == null)
                {
                    return null;
                }
                current = childTransform.gameObject;
            }
            
            return current;
        }
        
        public IEnumerable<GameObject> FindByPattern(string pattern, bool useRegex = false, int maxResults = 1000)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                yield break;
            }
            
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            var count = 0;
            
            if (useRegex)
            {
                var regex = new Regex(pattern);
                foreach (var go in allObjects)
                {
                    if (count >= maxResults) break;
                    
                    if (regex.IsMatch(go.name))
                    {
                        yield return go;
                        count++;
                    }
                }
            }
            else
            {
                // ワイルドカードをRegexに変換
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                var regex = new Regex(regexPattern);
                
                foreach (var go in allObjects)
                {
                    if (count >= maxResults) break;
                    
                    if (regex.IsMatch(go.name))
                    {
                        yield return go;
                        count++;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Asset専用のリソース解決実装。
    /// </summary>
    public class AssetResolver : IAssetResolver
    {
        public UnityEngine.Object Resolve(string identifier)
        {
            var result = TryResolve(identifier);
            if (result == null)
            {
                throw new InvalidOperationException($"Asset not found: '{identifier}'");
            }
            return result;
        }
        
        public UnityEngine.Object TryResolve(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }
            
            // パスで検索
            if (identifier.StartsWith("Assets/"))
            {
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(identifier);
            }
            
            // GUIDで検索
            if (identifier.Length == 32 && !identifier.Contains("/"))
            {
                return ResolveByGuid(identifier);
            }
            
            return null;
        }
        
        public bool Exists(string identifier)
        {
            return TryResolve(identifier) != null;
        }
        
        public IEnumerable<UnityEngine.Object> ResolveMany(params string[] identifiers)
        {
            return identifiers
                .Select(id => TryResolve(id))
                .Where(obj => obj != null);
        }
        
        public UnityEngine.Object ResolveByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
        
        public bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            
            // Assets/ で始まる必要がある
            if (!path.StartsWith("Assets/"))
            {
                return false;
            }
            
            // パストラバーサル攻撃を防ぐ
            if (path.Contains(".."))
            {
                return false;
            }
            
            // 不正な文字をチェック
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c)))
            {
                return false;
            }
            
            return true;
        }
        
        public Type GetAssetType(string path)
        {
            var asset = TryResolve(path);
            return asset?.GetType();
        }
    }
    
    /// <summary>
    /// Type専用のリソース解決実装。
    /// </summary>
    public class TypeResolver : ITypeResolver
    {
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly string[] _commonNamespaces = new[]
        {
            "UnityEngine",
            "UnityEngine.UI",
            "UnityEditor",
            "System",
            "System.Collections.Generic"
        };
        
        public Type Resolve(string identifier)
        {
            var result = TryResolve(identifier);
            if (result == null)
            {
                throw new InvalidOperationException($"Type not found: '{identifier}'");
            }
            return result;
        }
        
        public Type TryResolve(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }
            
            // キャッシュをチェック
            if (_typeCache.TryGetValue(identifier, out var cachedType))
            {
                return cachedType;
            }
            
            // 完全修飾名で検索
            var type = ResolveByFullName(identifier);
            if (type != null)
            {
                _typeCache[identifier] = type;
                return type;
            }
            
            // 短い名前で検索
            type = ResolveByShortName(identifier, _commonNamespaces);
            if (type != null)
            {
                _typeCache[identifier] = type;
            }
            
            return type;
        }
        
        public bool Exists(string identifier)
        {
            return TryResolve(identifier) != null;
        }
        
        public IEnumerable<Type> ResolveMany(params string[] identifiers)
        {
            return identifiers
                .Select(id => TryResolve(id))
                .Where(type => type != null);
        }
        
        public Type ResolveByFullName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
            {
                return null;
            }
            
            // 全アセンブリから型を検索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // アセンブリの読み込みに失敗した場合はスキップ
                    continue;
                }
            }
            
            return null;
        }
        
        public Type ResolveByShortName(string shortTypeName, params string[] searchNamespaces)
        {
            if (string.IsNullOrEmpty(shortTypeName))
            {
                return null;
            }
            
            // 各名前空間を試す
            foreach (var ns in searchNamespaces)
            {
                var fullName = $"{ns}.{shortTypeName}";
                var type = ResolveByFullName(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            
            return null;
        }
        
        public IEnumerable<Type> FindDerivedTypes(Type baseType)
        {
            if (baseType == null)
            {
                yield break;
            }
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 一部の型のロードに失敗した場合でも続行
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }
                
                foreach (var type in types)
                {
                    if (type == null || type == baseType)
                    {
                        continue;
                    }
                    
                    if (baseType.IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
            }
        }
        
        /// <summary>
        /// キャッシュをクリアします（テスト用）。
        /// </summary>
        public static void ClearCache()
        {
            _typeCache.Clear();
        }
    }
}

