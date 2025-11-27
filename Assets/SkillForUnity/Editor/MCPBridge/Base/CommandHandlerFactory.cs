using System;
using System.Collections.Generic;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コマンドハンドラーのファクトリークラス。
    /// ツール名からハンドラーインスタンスを取得します。
    /// </summary>
    public static class CommandHandlerFactory
    {
        private static readonly Dictionary<string, ICommandHandler> _handlers = new Dictionary<string, ICommandHandler>();
        private static readonly object _lock = new object();
        private static bool _initialized = false;
        
        /// <summary>
        /// ファクトリーを初期化します。
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            
            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }
                
                // 将来的に各ハンドラーを登録
                // 現時点では既存の実装を維持するため、登録は行いません
                
                /*
                // 例: ハンドラーの登録
                Register("sceneManage", new SceneCommandHandler());
                Register("gameObjectManage", new GameObjectCommandHandler());
                Register("componentManage", new ComponentCommandHandler());
                Register("assetManage", new AssetCommandHandler());
                Register("scriptableObjectManage", new ScriptableObjectCommandHandler());
                Register("prefabManage", new PrefabCommandHandler());
                Register("projectSettingsManage", new ProjectSettingsCommandHandler());
                */
                
                _initialized = true;
            }
        }
        
        /// <summary>
        /// ハンドラーを登録します。
        /// </summary>
        /// <param name="toolName">ツール名（例: "sceneManage"）</param>
        /// <param name="handler">ハンドラーインスタンス</param>
        public static void Register(string toolName, ICommandHandler handler)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentNullException(nameof(toolName));
            }
            
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            
            lock (_lock)
            {
                if (_handlers.ContainsKey(toolName))
                {
                    UnityEngine.Debug.LogWarning($"Handler for '{toolName}' is already registered. Overwriting.");
                }
                
                _handlers[toolName] = handler;
            }
        }
        
        /// <summary>
        /// ハンドラーを取得します。
        /// </summary>
        /// <param name="toolName">ツール名</param>
        /// <returns>ハンドラーインスタンス</returns>
        /// <exception cref="InvalidOperationException">ハンドラーが登録されていない場合</exception>
        public static ICommandHandler GetHandler(string toolName)
        {
            Initialize();
            
            lock (_lock)
            {
                if (!_handlers.ContainsKey(toolName))
                {
                    throw new InvalidOperationException(
                        $"No handler registered for tool: {toolName}. " +
                        $"Available handlers: {string.Join(", ", _handlers.Keys)}"
                    );
                }
                
                return _handlers[toolName];
            }
        }
        
        /// <summary>
        /// ハンドラーの取得を試みます。
        /// </summary>
        /// <param name="toolName">ツール名</param>
        /// <param name="handler">取得されたハンドラー</param>
        /// <returns>ハンドラーが存在する場合は true</returns>
        public static bool TryGetHandler(string toolName, out ICommandHandler handler)
        {
            Initialize();
            
            lock (_lock)
            {
                return _handlers.TryGetValue(toolName, out handler);
            }
        }
        
        /// <summary>
        /// ハンドラーが登録されているかチェックします。
        /// </summary>
        public static bool IsRegistered(string toolName)
        {
            Initialize();
            
            lock (_lock)
            {
                return _handlers.ContainsKey(toolName);
            }
        }
        
        /// <summary>
        /// 全てのハンドラーをクリアします（テスト用）。
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _initialized = false;
            }
        }
        
        /// <summary>
        /// 登録されている全てのツール名を取得します。
        /// </summary>
        public static IEnumerable<string> GetRegisteredToolNames()
        {
            Initialize();
            
            lock (_lock)
            {
                return new List<string>(_handlers.Keys);
            }
        }
        
        /// <summary>
        /// ファクトリーの統計情報を取得します。
        /// </summary>
        public static Dictionary<string, object> GetStatistics()
        {
            Initialize();
            
            lock (_lock)
            {
                var stats = new Dictionary<string, object>
                {
                    ["totalHandlers"] = _handlers.Count,
                    ["initialized"] = _initialized,
                    ["registeredHandlers"] = new List<Dictionary<string, object>>()
                };
                
                var handlersList = (List<Dictionary<string, object>>)stats["registeredHandlers"];
                
                foreach (var kvp in _handlers)
                {
                    handlersList.Add(new Dictionary<string, object>
                    {
                        ["toolName"] = kvp.Key,
                        ["category"] = kvp.Value.Category,
                        ["version"] = kvp.Value.Version,
                        ["supportedOperations"] = new List<string>(kvp.Value.SupportedOperations)
                    });
                }
                
                return stats;
            }
        }
    }
}

