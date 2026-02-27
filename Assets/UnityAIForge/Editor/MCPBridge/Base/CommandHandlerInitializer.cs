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

                // 開発サイクル・ビジュアル制御ツールのハンドラーを登録
                RegisterDevCycleAndVisualHandlers();

                // ハイレベルGameKitツールのハンドラーを登録
                RegisterGameKitHandlers();
                
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
            CommandHandlerFactory.Register("cameraRig", new CameraRigHandler());
            CommandHandlerFactory.Register("uiFoundation", new UIFoundationHandler());
            CommandHandlerFactory.Register("uiHierarchy", new UIHierarchyHandler());
            CommandHandlerFactory.Register("uiState", new UIStateHandler());
            CommandHandlerFactory.Register("uiNavigation", new UINavigationHandler());
            CommandHandlerFactory.Register("inputProfile", new InputProfileHandler());
            CommandHandlerFactory.Register("tilemapBundle", new TilemapBundleHandler());
            CommandHandlerFactory.Register("sprite2DBundle", new Sprite2DBundleHandler());
            CommandHandlerFactory.Register("animation2DBundle", new Animation2DBundleHandler());

            // UI Toolkit tools
            CommandHandlerFactory.Register("uitkDocument", new UITKDocumentHandler());
            CommandHandlerFactory.Register("uitkAsset", new UITKAssetHandler());

            // Physics & NavMesh
            CommandHandlerFactory.Register("physicsBundle", new PhysicsBundleHandler());
            CommandHandlerFactory.Register("navmeshBundle", new NavMeshBundleHandler());
        }

        /// <summary>
        /// 開発サイクル基盤・ビジュアル制御ツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterDevCycleAndVisualHandlers()
        {
            // 開発サイクル基盤
            CommandHandlerFactory.Register("playModeControl", new PlayModeControlHandler());
            CommandHandlerFactory.Register("consoleLog", new ConsoleLogHandler());

            // ビジュアル制御
            CommandHandlerFactory.Register("materialBundle", new MaterialBundleHandler());
            CommandHandlerFactory.Register("lightBundle", new LightBundleHandler());
            CommandHandlerFactory.Register("particleBundle", new ParticleBundleHandler());

            // アニメーション・イベント
            CommandHandlerFactory.Register("animation3DBundle", new Animation3DBundleHandler());
            CommandHandlerFactory.Register("eventWiring", new EventWiringHandler());
        }

        /// <summary>
        /// ハイレベルGameKitツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterGameKitHandlers()
        {
            // UI Pillar
            CommandHandlerFactory.Register("gamekitUICommand", new Handlers.GameKit.GameKitUICommandHandler());
            CommandHandlerFactory.Register("gamekitUIBinding", new Handlers.GameKit.GameKitUIBindingHandler());
            CommandHandlerFactory.Register("gamekitUIList", new Handlers.GameKit.GameKitUIListHandler());
            CommandHandlerFactory.Register("gamekitUISlot", new Handlers.GameKit.GameKitUISlotHandler());
            CommandHandlerFactory.Register("gamekitUISelection", new Handlers.GameKit.GameKitUISelectionHandler());

            // Presentation Pillar
            CommandHandlerFactory.Register("gamekitAnimationSync", new Handlers.GameKit.GameKitAnimationSyncHandler());
            CommandHandlerFactory.Register("gamekitEffect", new Handlers.GameKit.GameKitEffectHandler());
            CommandHandlerFactory.Register("gamekitFeedback", new Handlers.GameKit.GameKitFeedbackHandler());
            CommandHandlerFactory.Register("gamekitVFX", new Handlers.GameKit.GameKitVFXHandler());
            CommandHandlerFactory.Register("gamekitAudio", new Handlers.GameKit.GameKitAudioHandler());

            // Systems (Pool & Data)
            CommandHandlerFactory.Register("gamekitPool", new Handlers.GameKit.GameKitPoolHandler());
            CommandHandlerFactory.Register("gamekitData", new Handlers.GameKit.GameKitDataHandler());

            // Logic Pillar — 整合性検証・依存関係/参照解析・型カタログ
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

