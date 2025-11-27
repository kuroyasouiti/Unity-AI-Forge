namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// リソース解決インターフェース。
    /// パス、GUID、名前などからUnityリソースを解決します。
    /// </summary>
    /// <typeparam name="T">解決するリソースの型</typeparam>
    public interface IResourceResolver<T>
    {
        /// <summary>
        /// リソースを識別子から解決します。
        /// </summary>
        /// <param name="identifier">識別子（パス、GUID、名前など）</param>
        /// <returns>解決されたリソース</returns>
        /// <exception cref="System.InvalidOperationException">リソースが見つからない場合</exception>
        T Resolve(string identifier);
        
        /// <summary>
        /// リソースを識別子から解決します（失敗時はnullを返す）。
        /// </summary>
        /// <param name="identifier">識別子</param>
        /// <returns>解決されたリソース、または null</returns>
        T TryResolve(string identifier);
        
        /// <summary>
        /// リソースが存在するかチェックします。
        /// </summary>
        /// <param name="identifier">識別子</param>
        /// <returns>リソースが存在する場合は true</returns>
        bool Exists(string identifier);
        
        /// <summary>
        /// 複数の識別子からリソースを解決します。
        /// </summary>
        /// <param name="identifiers">識別子のリスト</param>
        /// <returns>解決されたリソースのリスト（nullは含まない）</returns>
        System.Collections.Generic.IEnumerable<T> ResolveMany(params string[] identifiers);
    }
    
    /// <summary>
    /// GameObject専用のリソース解決インターフェース。
    /// </summary>
    public interface IGameObjectResolver : IResourceResolver<UnityEngine.GameObject>
    {
        /// <summary>
        /// 階層パスからGameObjectを解決します。
        /// </summary>
        /// <param name="hierarchyPath">階層パス（例: "Root/Child/Button"）</param>
        /// <returns>解決されたGameObject</returns>
        UnityEngine.GameObject ResolveByHierarchyPath(string hierarchyPath);
        
        /// <summary>
        /// パターンマッチングでGameObjectを検索します。
        /// </summary>
        /// <param name="pattern">パターン（ワイルドカードまたは正規表現）</param>
        /// <param name="useRegex">正規表現として扱うかどうか</param>
        /// <param name="maxResults">最大結果数</param>
        /// <returns>マッチしたGameObjectのリスト</returns>
        System.Collections.Generic.IEnumerable<UnityEngine.GameObject> FindByPattern(
            string pattern, 
            bool useRegex = false, 
            int maxResults = 1000
        );
    }
    
    /// <summary>
    /// Asset専用のリソース解決インターフェース。
    /// </summary>
    public interface IAssetResolver : IResourceResolver<UnityEngine.Object>
    {
        /// <summary>
        /// GUIDからアセットを解決します。
        /// </summary>
        /// <param name="guid">アセットGUID</param>
        /// <returns>解決されたアセット</returns>
        UnityEngine.Object ResolveByGuid(string guid);
        
        /// <summary>
        /// アセットパスを検証します。
        /// </summary>
        /// <param name="path">アセットパス</param>
        /// <returns>パスが有効な場合は true</returns>
        bool ValidatePath(string path);
        
        /// <summary>
        /// アセットの型を取得します。
        /// </summary>
        /// <param name="path">アセットパス</param>
        /// <returns>アセットの型</returns>
        System.Type GetAssetType(string path);
    }
    
    /// <summary>
    /// Type専用のリソース解決インターフェース。
    /// </summary>
    public interface ITypeResolver : IResourceResolver<System.Type>
    {
        /// <summary>
        /// 型名から型を解決します（名前空間付き）。
        /// </summary>
        /// <param name="fullTypeName">完全修飾型名（例: "UnityEngine.Rigidbody"）</param>
        /// <returns>解決された型</returns>
        System.Type ResolveByFullName(string fullTypeName);
        
        /// <summary>
        /// 短い型名から型を解決します（名前空間なし）。
        /// </summary>
        /// <param name="shortTypeName">短い型名（例: "Rigidbody"）</param>
        /// <param name="searchNamespaces">検索する名前空間のリスト</param>
        /// <returns>解決された型</returns>
        System.Type ResolveByShortName(
            string shortTypeName, 
            params string[] searchNamespaces
        );
        
        /// <summary>
        /// 指定された基底型を継承する全ての型を検索します。
        /// </summary>
        /// <param name="baseType">基底型</param>
        /// <returns>継承する型のリスト</returns>
        System.Collections.Generic.IEnumerable<System.Type> FindDerivedTypes(System.Type baseType);
    }
}

