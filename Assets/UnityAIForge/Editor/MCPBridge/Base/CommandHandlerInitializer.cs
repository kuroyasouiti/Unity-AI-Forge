using MCP.Editor.Handlers;
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
        private static bool _isInitializing = false;
        private static bool _hasInitialized = false;

        /// <summary>
        /// 静的コンストラクタ。Unity起動時に自動実行されます。
        /// </summary>
        static CommandHandlerInitializer()
        {
            // コンパイル完了後に初期化（delayCallはUnity再コンパイル時用）
            EditorApplication.delayCall += () =>
            {
                // delayCall経由の場合は強制再初期化（再コンパイル対応）
                _hasInitialized = false;
                InitializeHandlers();
            };
        }

        /// <summary>
        /// 全てのコマンドハンドラーを初期化して登録します。
        /// 重複呼び出しは無視されます。
        /// </summary>
        public static void InitializeHandlers()
        {
            // 重複初期化防止
            if (_hasInitialized || _isInitializing)
            {
                return;
            }

            _isInitializing = true;

            try
            {
                Debug.Log("[CommandHandlerInitializer] Initializing command handlers...");

                // 既存のハンドラーをクリア（再初期化時）
                CommandHandlerFactory.Clear();
                
                // Phase 3で実装済みのハンドラーを登録
                RegisterPhase3Handlers();
                
                // Phase 5で実装済みのハンドラーを登録
                RegisterPhase5Handlers();
                
                // Phase 7で実装済みのハンドラーを登録
                RegisterPhase7Handlers();

                // ミドルレベルツールのハンドラーを登録
                RegisterMidLevelHandlers();

                // ハイレベルGameKitツールのハンドラーを登録
                RegisterGameKitHandlers();
                
                // 統計情報をログ出力
                var stats = CommandHandlerFactory.GetStatistics();
                Debug.Log($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} command handlers");
                
                // 詳細ログ
                var handlers = (System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>)stats["registeredHandlers"];
                foreach (var handlerInfo in handlers)
                {
                    Debug.Log($"  - {handlerInfo["toolName"]}: {handlerInfo["category"]} (v{handlerInfo["version"]})");
                }

                _hasInitialized = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CommandHandlerInitializer] Failed to initialize handlers: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                _isInitializing = false;
            }
        }
        
        /// <summary>
        /// Phase 3で実装されたハンドラーを登録します。
        /// </summary>
        private static void RegisterPhase3Handlers()
        {
            // Utility Handlers
            CommandHandlerFactory.Register("pingUnityEditor", new PingHandler());
            CommandHandlerFactory.Register("compilationAwait", new CompilationAwaitHandler());

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
            // ScriptableObject Handler
            CommandHandlerFactory.Register("scriptableObjectManage", new ScriptableObjectCommandHandler());
            
            // Prefab Handler
            CommandHandlerFactory.Register("prefabManage", new PrefabCommandHandler());
            
            // Vector Sprite Converter
            CommandHandlerFactory.Register("vectorSpriteConvert", new VectorSpriteConvertHandler());
        }
        
        /// <summary>
        /// Phase 7で実装されたハンドラーを登録します（Settings & Utilities関連）。
        /// </summary>
        private static void RegisterPhase7Handlers()
        {
            // Project Settings Handler
            CommandHandlerFactory.Register("projectSettingsManage", new Handlers.Settings.ProjectSettingsManageHandler());
        }

        /// <summary>
        /// ミドルレベルツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterMidLevelHandlers()
        {
            CommandHandlerFactory.Register("transformBatch", new TransformBatchHandler());
            CommandHandlerFactory.Register("rectTransformBatch", new RectTransformBatchHandler());
            CommandHandlerFactory.Register("physicsBundle", new PhysicsBundleHandler());
            CommandHandlerFactory.Register("cameraRig", new CameraRigHandler());
            CommandHandlerFactory.Register("uiFoundation", new UIFoundationHandler());
            CommandHandlerFactory.Register("uiHierarchy", new UIHierarchyHandler());
            CommandHandlerFactory.Register("uiState", new UIStateHandler());
            CommandHandlerFactory.Register("uiNavigation", new UINavigationHandler());
            CommandHandlerFactory.Register("audioSourceBundle", new AudioSourceBundleHandler());
            CommandHandlerFactory.Register("inputProfile", new InputProfileHandler());
            CommandHandlerFactory.Register("characterControllerBundle", new CharacterControllerBundleHandler());
            CommandHandlerFactory.Register("tilemapBundle", new TilemapBundleHandler());
            CommandHandlerFactory.Register("sprite2DBundle", new Sprite2DBundleHandler());
            CommandHandlerFactory.Register("animation2DBundle", new Animation2DBundleHandler());
        }

        /// <summary>
        /// ハイレベルGameKitツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterGameKitHandlers()
        {
            CommandHandlerFactory.Register("gamekitActor", new Handlers.GameKit.GameKitActorHandler());
            CommandHandlerFactory.Register("gamekitManager", new Handlers.GameKit.GameKitManagerHandler());
            CommandHandlerFactory.Register("gamekitInteraction", new Handlers.GameKit.GameKitInteractionHandler());
            CommandHandlerFactory.Register("gamekitUICommand", new Handlers.GameKit.GameKitUICommandHandler());
            CommandHandlerFactory.Register("gamekitMachinations", new Handlers.GameKit.GameKitMachinationsHandler());
            CommandHandlerFactory.Register("gamekitSceneFlow", new Handlers.GameKit.GameKitSceneFlowHandler());
        }
    }
}

