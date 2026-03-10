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
        /// 初期化状態をリセットします（テスト用）。
        /// </summary>
        public static void ResetInitializationState()
        {
            _hasInitialized = false;
            _isInitializing = false;
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
                // 既存のハンドラーをクリア（再初期化時）
                CommandHandlerFactory.Clear();

                // ユーティリティハンドラーを登録
                RegisterUtilityHandlers();

                // ローレベルCRUDハンドラーを登録
                RegisterLowLevelHandlers();

                // ミドルレベルツールのハンドラーを登録
                RegisterMidLevelHandlers();

                // 開発サイクル基盤ツールのハンドラーを登録
                RegisterDevCycleHandlers();

                // ハイレベルツールのハンドラーを登録
                RegisterHighLevelHandlers();
                
                var stats = CommandHandlerFactory.GetStatistics();
                _hasInitialized = true;

                Debug.Log($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} command handlers");
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
        /// ユーティリティハンドラーを登録します（Ping、コンパイル待機）。
        /// </summary>
        private static void RegisterUtilityHandlers()
        {
            CommandHandlerFactory.Register("pingUnityEditor", new PingHandler());
            CommandHandlerFactory.Register("compilationAwait", new CompilationAwaitHandler());
        }

        /// <summary>
        /// ローレベルCRUDハンドラーを登録します。
        /// </summary>
        private static void RegisterLowLevelHandlers()
        {
            CommandHandlerFactory.Register("sceneManage", new SceneCommandHandler());
            CommandHandlerFactory.Register("gameObjectManage", new GameObjectCommandHandler());
            CommandHandlerFactory.Register("componentManage", new ComponentCommandHandler());
            CommandHandlerFactory.Register("assetManage", new AssetCommandHandler());
            CommandHandlerFactory.Register("scriptableObjectManage", new ScriptableObjectCommandHandler());
            CommandHandlerFactory.Register("prefabManage", new PrefabCommandHandler());
            CommandHandlerFactory.Register("vectorSpriteConvert", new VectorSpriteConvertHandler());
            CommandHandlerFactory.Register("projectSettingsManage", new Handlers.Settings.ProjectSettingsManageHandler());
        }

        /// <summary>
        /// ミドルレベルツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterMidLevelHandlers()
        {
            CommandHandlerFactory.Register("transformBatch", new TransformBatchHandler());
            CommandHandlerFactory.Register("rectTransformBatch", new RectTransformBatchHandler());
            CommandHandlerFactory.Register("cameraBundle", new CameraBundleHandler());
            CommandHandlerFactory.Register("uiFoundation", new UIFoundationHandler());
            CommandHandlerFactory.Register("uiState", new UIStateHandler());
            CommandHandlerFactory.Register("uiNavigation", new UINavigationHandler());
            CommandHandlerFactory.Register("inputProfile", new InputProfileHandler());
            CommandHandlerFactory.Register("tilemapBundle", new TilemapBundleHandler());
            CommandHandlerFactory.Register("sprite2DBundle", new Sprite2DBundleHandler());
            CommandHandlerFactory.Register("animation2DBundle", new Animation2DBundleHandler());

            // UI Toolkit tools
            CommandHandlerFactory.Register("uitkDocument", new UITKDocumentHandler());
            CommandHandlerFactory.Register("uitkAsset", new UITKAssetHandler());

            // UI Convert
            CommandHandlerFactory.Register("uiConvert", new UIConvertHandler());

            // Physics & NavMesh
            CommandHandlerFactory.Register("physicsBundle", new PhysicsBundleHandler());
            CommandHandlerFactory.Register("navmeshBundle", new NavMeshBundleHandler());

            // Visual control
            CommandHandlerFactory.Register("materialBundle", new MaterialBundleHandler());
            CommandHandlerFactory.Register("lightBundle", new LightBundleHandler());
            CommandHandlerFactory.Register("particleBundle", new ParticleBundleHandler());

            // Animation 3D
            CommandHandlerFactory.Register("animation3DBundle", new Animation3DBundleHandler());
        }

        /// <summary>
        /// 開発サイクル基盤ツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterDevCycleHandlers()
        {
            CommandHandlerFactory.Register("playModeControl", new PlayModeControlHandler());
            CommandHandlerFactory.Register("consoleLog", new ConsoleLogHandler());
            CommandHandlerFactory.Register("eventWiring", new EventWiringHandler());
        }

        /// <summary>
        /// ハイレベルツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterHighLevelHandlers()
        {
            // GameKit UI (unified dispatcher → 5 internal sub-handlers)
            CommandHandlerFactory.Register("gamekitUI", new Handlers.HighLevel.GameKitUIHandler());

            // GameKit Data (unified: pool + eventChannel + dataContainer + runtimeSet)
            CommandHandlerFactory.Register("gamekitData", new Handlers.HighLevel.GameKitDataHandler());

            // Logic — 整合性検証・依存関係/参照解析・型カタログ
            CommandHandlerFactory.Register("sceneIntegrity", new Handlers.HighLevel.SceneIntegrityHandler());
            CommandHandlerFactory.Register("classDependencyGraph", new Handlers.HighLevel.ClassDependencyGraphHandler());
            CommandHandlerFactory.Register("classCatalog", new Handlers.HighLevel.ClassCatalogHandler());
            CommandHandlerFactory.Register("sceneReferenceGraph", new Handlers.HighLevel.SceneReferenceGraphHandler());
            CommandHandlerFactory.Register("sceneRelationshipGraph", new Handlers.HighLevel.SceneRelationshipGraphHandler());
            CommandHandlerFactory.Register("sceneDependency", new Handlers.HighLevel.SceneDependencyHandler());
            CommandHandlerFactory.Register("scriptSyntax", new Handlers.HighLevel.ScriptSyntaxHandler());
        }
    }
}

