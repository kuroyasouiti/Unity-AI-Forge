using System;
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

                int failedCount = 0;

                // ユーティリティハンドラーを登録
                failedCount += RegisterUtilityHandlers();

                // ローレベルCRUDハンドラーを登録
                failedCount += RegisterLowLevelHandlers();

                // ミドルレベルツールのハンドラーを登録
                failedCount += RegisterMidLevelHandlers();

                // 開発サイクル・ビジュアル制御ツールのハンドラーを登録
                failedCount += RegisterDevCycleAndVisualHandlers();

                // ハイレベルツールのハンドラーを登録
                failedCount += RegisterHighLevelHandlers();

                var stats = CommandHandlerFactory.GetStatistics();
                _hasInitialized = true;

                if (failedCount > 0)
                {
                    Debug.LogWarning($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} handlers ({failedCount} failed)");
                }
                else
                {
                    Debug.Log($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} command handlers");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandHandlerInitializer] Fatal initialization error: {ex.Message}");
                Debug.LogException(ex);
                _hasInitialized = true; // Prevent infinite retry loops
            }
            finally
            {
                _isInitializing = false;
            }
        }
        
        /// <summary>
        /// 個別ハンドラー登録のヘルパー。失敗してもログを出して他のハンドラーの登録を続行します。
        /// </summary>
        private static bool TryRegister(string name, Func<Interfaces.ICommandHandler> factory)
        {
            try
            {
                CommandHandlerFactory.Register(name, factory());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandHandlerInitializer] Failed to register '{name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ユーティリティハンドラーを登録します（Ping、コンパイル待機）。
        /// </summary>
        private static int RegisterUtilityHandlers()
        {
            int failed = 0;
            if (!TryRegister("pingUnityEditor", () => new PingHandler())) failed++;
            if (!TryRegister("compilationAwait", () => new CompilationAwaitHandler())) failed++;
            return failed;
        }

        /// <summary>
        /// ローレベルCRUDハンドラーを登録します。
        /// </summary>
        private static int RegisterLowLevelHandlers()
        {
            int failed = 0;
            if (!TryRegister("sceneManage", () => new SceneCommandHandler())) failed++;
            if (!TryRegister("gameObjectManage", () => new GameObjectCommandHandler())) failed++;
            if (!TryRegister("componentManage", () => new ComponentCommandHandler())) failed++;
            if (!TryRegister("assetManage", () => new AssetCommandHandler())) failed++;
            if (!TryRegister("scriptableObjectManage", () => new ScriptableObjectCommandHandler())) failed++;
            if (!TryRegister("prefabManage", () => new PrefabCommandHandler())) failed++;
            if (!TryRegister("vectorSpriteConvert", () => new VectorSpriteConvertHandler())) failed++;
            if (!TryRegister("projectSettingsManage", () => new Handlers.Settings.ProjectSettingsManageHandler())) failed++;
            return failed;
        }

        /// <summary>
        /// ミドルレベルツールのハンドラーを登録します。
        /// </summary>
        private static int RegisterMidLevelHandlers()
        {
            int failed = 0;
            if (!TryRegister("transformBatch", () => new TransformBatchHandler())) failed++;
            if (!TryRegister("rectTransformBatch", () => new RectTransformBatchHandler())) failed++;
            if (!TryRegister("cameraBundle", () => new CameraBundleHandler())) failed++;
            if (!TryRegister("uiFoundation", () => new UIFoundationHandler())) failed++;
            if (!TryRegister("uiState", () => new UIStateHandler())) failed++;
            if (!TryRegister("uiNavigation", () => new UINavigationHandler())) failed++;
            if (!TryRegister("inputProfile", () => new InputProfileHandler())) failed++;
            if (!TryRegister("tilemapBundle", () => new TilemapBundleHandler())) failed++;
            if (!TryRegister("sprite2DBundle", () => new Sprite2DBundleHandler())) failed++;
            if (!TryRegister("animation2DBundle", () => new Animation2DBundleHandler())) failed++;

            // UI Toolkit tools
            if (!TryRegister("uitkDocument", () => new UITKDocumentHandler())) failed++;
            if (!TryRegister("uitkAsset", () => new UITKAssetHandler())) failed++;

            // UI Convert
            if (!TryRegister("uiConvert", () => new UIConvertHandler())) failed++;

            // Physics & NavMesh
            if (!TryRegister("physicsBundle", () => new PhysicsBundleHandler())) failed++;
            if (!TryRegister("navmeshBundle", () => new NavMeshBundleHandler())) failed++;
            return failed;
        }

        /// <summary>
        /// 開発サイクル基盤・ビジュアル制御ツールのハンドラーを登録します。
        /// </summary>
        private static int RegisterDevCycleAndVisualHandlers()
        {
            int failed = 0;
            // 開発サイクル基盤
            if (!TryRegister("playModeControl", () => new PlayModeControlHandler())) failed++;
            if (!TryRegister("consoleLog", () => new ConsoleLogHandler())) failed++;

            // ビジュアル制御
            if (!TryRegister("materialBundle", () => new MaterialBundleHandler())) failed++;
            if (!TryRegister("lightBundle", () => new LightBundleHandler())) failed++;
            if (!TryRegister("particleBundle", () => new ParticleBundleHandler())) failed++;

            // アニメーション・イベント
            if (!TryRegister("animation3DBundle", () => new Animation3DBundleHandler())) failed++;
            if (!TryRegister("eventWiring", () => new EventWiringHandler())) failed++;
            return failed;
        }

        /// <summary>
        /// ハイレベルツールのハンドラーを登録します。
        /// </summary>
        private static int RegisterHighLevelHandlers()
        {
            int failed = 0;
            // GameKit UI (unified dispatcher → 5 internal sub-handlers)
            if (!TryRegister("gamekitUI", () => new Handlers.HighLevel.GameKitUIHandler())) failed++;

            // GameKit Data (unified: pool + eventChannel + dataContainer + runtimeSet)
            if (!TryRegister("gamekitData", () => new Handlers.HighLevel.GameKitDataHandler())) failed++;

            // Logic — 整合性検証・依存関係/参照解析・型カタログ・空間解析
            if (!TryRegister("spatialAnalysis", () => new Handlers.HighLevel.SpatialAnalysisHandler())) failed++;
            if (!TryRegister("sceneIntegrity", () => new Handlers.HighLevel.SceneIntegrityHandler())) failed++;
            if (!TryRegister("classDependencyGraph", () => new Handlers.HighLevel.ClassDependencyGraphHandler())) failed++;
            if (!TryRegister("classCatalog", () => new Handlers.HighLevel.ClassCatalogHandler())) failed++;
            if (!TryRegister("sceneReferenceGraph", () => new Handlers.HighLevel.SceneReferenceGraphHandler())) failed++;
            if (!TryRegister("sceneRelationshipGraph", () => new Handlers.HighLevel.SceneRelationshipGraphHandler())) failed++;
            if (!TryRegister("sceneDependency", () => new Handlers.HighLevel.SceneDependencyHandler())) failed++;
            if (!TryRegister("scriptSyntax", () => new Handlers.HighLevel.ScriptSyntaxHandler())) failed++;
            return failed;
        }
    }
}

