using MCP.Editor.Handlers;
using SkillForUnity.MCPBridge.Handlers;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コマンドハンドラーの初期化クラス。
    /// Unity起動時またはコンパイル後に自動的にハンドラーを登録します。
    /// </summary>
    [InitializeOnLoad]
    public static class CommandHandlerInitializer
    {
        /// <summary>
        /// 静的コンストラクタ。Unity起動時に自動実行されます。
        /// </summary>
        static CommandHandlerInitializer()
        {
            // コンパイル完了後に初期化
            EditorApplication.delayCall += InitializeHandlers;
        }
        
        /// <summary>
        /// 全てのコマンドハンドラーを初期化して登録します。
        /// </summary>
        public static void InitializeHandlers()
        {
            try
            {
                Debug.Log("[CommandHandlerInitializer] Initializing command handlers...");
                
                // 既存のハンドラーをクリア（再初期化時）
                CommandHandlerFactory.Clear();
                
                // Phase 3で実装済みのハンドラーを登録
                RegisterPhase3Handlers();
                
                // Phase 5で実装済みのハンドラーを登録
                RegisterPhase5Handlers();
                
                // Phase 6で実装済みのハンドラーを登録
                RegisterPhase6Handlers();
                
                // Phase 6bで実装済みのハンドラーを登録
                RegisterPhase6BHandlers();
                
                // Phase 7で実装済みのハンドラーを登録
                RegisterPhase7Handlers();
                
                // Phase 8で実装済みのハンドラーを登録
                RegisterPhase8Handlers();
                
                // 統計情報をログ出力
                var stats = CommandHandlerFactory.GetStatistics();
                Debug.Log($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} command handlers");
                
                // 詳細ログ
                var handlers = (System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>)stats["registeredHandlers"];
                foreach (var handlerInfo in handlers)
                {
                    Debug.Log($"  - {handlerInfo["toolName"]}: {handlerInfo["category"]} (v{handlerInfo["version"]})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CommandHandlerInitializer] Failed to initialize handlers: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Phase 3で実装されたハンドラーを登録します。
        /// </summary>
        private static void RegisterPhase3Handlers()
        {
            // Scene Handler
            CommandHandlerFactory.Register("sceneManage", new SceneCommandHandler());
            
            // GameObject Handler
            CommandHandlerFactory.Register("gameObjectManage", new GameObjectCommandHandler());
            
            // Component Handler
            CommandHandlerFactory.Register("componentManage", new ComponentCommandHandler());
            
            // Asset Handler
            CommandHandlerFactory.Register("assetManage", new AssetCommandHandler());
        }
        
        /// <summary>
        /// Phase 5で実装されたハンドラーを登録します。
        /// </summary>
        private static void RegisterPhase5Handlers()
        {
            // Prefab Handler
            CommandHandlerFactory.Register("prefabManage", new PrefabCommandHandler());
            
            // ScriptableObject Handler
            CommandHandlerFactory.Register("scriptableObjectManage", new ScriptableObjectCommandHandler());
        }
        
        /// <summary>
        /// Phase 6で実装されたハンドラーを登録します。
        /// </summary>
        private static void RegisterPhase6Handlers()
        {
            // Template Handler (consolidated 6 template-related tools)
            var templateHandler = new TemplateCommandHandler();
            CommandHandlerFactory.Register("sceneQuickSetup", templateHandler);
            CommandHandlerFactory.Register("gameObjectCreateFromTemplate", templateHandler);
            CommandHandlerFactory.Register("designPatternGenerate", templateHandler);
            CommandHandlerFactory.Register("scriptTemplateGenerate", templateHandler);
            CommandHandlerFactory.Register("templateManage", templateHandler);
            CommandHandlerFactory.Register("menuHierarchyCreate", templateHandler);
        }
        
        /// <summary>
        /// Phase 6bで実装されたハンドラーを登録します（UGUI関連）。
        /// </summary>
        private static void RegisterPhase6BHandlers()
        {
            // UGUI Handlers
            CommandHandlerFactory.Register("uguiManage", new UguiManageCommandHandler());
            CommandHandlerFactory.Register("uguiCreateFromTemplate", new UguiCreateFromTemplateHandler());
            CommandHandlerFactory.Register("uguiLayoutManage", new UguiLayoutManageHandler());
            CommandHandlerFactory.Register("uguiDetectOverlaps", new UguiDetectOverlapsHandler());
        }
        
        /// <summary>
        /// Phase 7で実装されたハンドラーを登録します（Settings & Utilities関連）。
        /// </summary>
        private static void RegisterPhase7Handlers()
        {
            // Tag/Layer Management Handler
            CommandHandlerFactory.Register("tagLayerManage", new Handlers.Settings.TagLayerManageHandler());
            
            // Project Settings Handler
            CommandHandlerFactory.Register("projectSettingsManage", new Handlers.Settings.ProjectSettingsManageHandler());
            
            // Render Pipeline Handler
            CommandHandlerFactory.Register("renderPipelineManage", new Handlers.Settings.RenderPipelineManageHandler());
            
            // Constant Convert Handler
            CommandHandlerFactory.Register("constantConvert", new Handlers.Settings.ConstantConvertHandler());
            
            // Compilation Await Handler
            CommandHandlerFactory.Register("compilationAwait", new CompilationAwaitHandler());
        }
        
        /// <summary>
        /// Phase 8で実装されたハンドラーを登録します（Vector & Sprite関連）。
        /// </summary>
        private static void RegisterPhase8Handlers()
        {
            // Vector to Sprite Conversion Handler
            CommandHandlerFactory.Register("vectorSpriteConvert", new VectorSpriteConvertHandler());
        }
        
        /// <summary>
        /// 残りのハンドラーを登録します（Phase 9以降で実装予定）。
        /// </summary>
        private static void RegisterRemainingHandlers()
        {
            // TODO: Phase 9以降で実装
        }
    }
}

