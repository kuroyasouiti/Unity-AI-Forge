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
                
                // Phase 3で実装済みのハンドラーを登録
                RegisterPhase3Handlers();
                
                // Phase 5で実装済みのハンドラーを登録
                RegisterPhase5Handlers();
                
                // Phase 7で実装済みのハンドラーを登録
                RegisterPhase7Handlers();

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
        /// 開発サイクル基盤・ビジュアル制御ツールのハンドラーを登録します。
        /// ROADMAP_MCP_TOOLS.md に基づく実装です。
        /// </summary>
        private static void RegisterDevCycleAndVisualHandlers()
        {
            // Phase 1: 開発サイクル基盤 (最優先)
            CommandHandlerFactory.Register("playModeControl", new PlayModeControlHandler());
            CommandHandlerFactory.Register("consoleLog", new ConsoleLogHandler());

            // Phase 2: ビジュアル制御 (重要)
            CommandHandlerFactory.Register("materialBundle", new MaterialBundleHandler());
            CommandHandlerFactory.Register("lightBundle", new LightBundleHandler());
            CommandHandlerFactory.Register("particleBundle", new ParticleBundleHandler());

            // Phase 3: アニメーション・イベント (推奨)
            CommandHandlerFactory.Register("animation3DBundle", new Animation3DBundleHandler());
            CommandHandlerFactory.Register("eventWiring", new EventWiringHandler());
        }

        /// <summary>
        /// ハイレベルGameKitツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterGameKitHandlers()
        {
            // Core GameKit handlers
            CommandHandlerFactory.Register("gamekitActor", new Handlers.GameKit.GameKitActorHandler());
            CommandHandlerFactory.Register("gamekitManager", new Handlers.GameKit.GameKitManagerHandler());
            CommandHandlerFactory.Register("gamekitInteraction", new Handlers.GameKit.GameKitInteractionHandler());
            CommandHandlerFactory.Register("gamekitUICommand", new Handlers.GameKit.GameKitUICommandHandler());
            CommandHandlerFactory.Register("gamekitMachinations", new Handlers.GameKit.GameKitMachinationsHandler());
            CommandHandlerFactory.Register("gamekitSceneFlow", new Handlers.GameKit.GameKitSceneFlowHandler());

            // Phase 1 GameKit handlers - Common game mechanics
            CommandHandlerFactory.Register("gamekitHealth", new Handlers.GameKit.GameKitHealthHandler());
            CommandHandlerFactory.Register("gamekitSpawner", new Handlers.GameKit.GameKitSpawnerHandler());
            CommandHandlerFactory.Register("gamekitTimer", new Handlers.GameKit.GameKitTimerHandler());
            CommandHandlerFactory.Register("gamekitAI", new Handlers.GameKit.GameKitAIHandler());

            // Phase 2 GameKit handlers - Additional game mechanics
            CommandHandlerFactory.Register("gamekitCollectible", new Handlers.GameKit.GameKitCollectibleHandler());
            CommandHandlerFactory.Register("gamekitProjectile", new Handlers.GameKit.GameKitProjectileHandler());
            CommandHandlerFactory.Register("gamekitWaypoint", new Handlers.GameKit.GameKitWaypointHandler());
            CommandHandlerFactory.Register("gamekitTriggerZone", new Handlers.GameKit.GameKitTriggerZoneHandler());

            // Phase 3 GameKit handlers - Animation & Effects
            CommandHandlerFactory.Register("gamekitAnimationSync", new Handlers.GameKit.GameKitAnimationSyncHandler());
            CommandHandlerFactory.Register("gamekitEffect", new Handlers.GameKit.GameKitEffectHandler());

            // Phase 4 GameKit handlers - Persistence & Inventory
            CommandHandlerFactory.Register("gamekitSave", new Handlers.GameKit.GameKitSaveHandler());
            CommandHandlerFactory.Register("gamekitInventory", new Handlers.GameKit.GameKitInventoryHandler());

            // Phase 5 GameKit handlers - Story & Quest Systems
            CommandHandlerFactory.Register("gamekitDialogue", new Handlers.GameKit.GameKitDialogueHandler());
            CommandHandlerFactory.Register("gamekitQuest", new Handlers.GameKit.GameKitQuestHandler());
            CommandHandlerFactory.Register("gamekitStatusEffect", new Handlers.GameKit.GameKitStatusEffectHandler());

            // 3-Pillar Architecture handlers (v2.7.0)
            // UI Pillar
            CommandHandlerFactory.Register("gamekitUIBinding", new Handlers.GameKit.GameKitUIBindingHandler());
            CommandHandlerFactory.Register("gamekitUIList", new Handlers.GameKit.GameKitUIListHandler());
            CommandHandlerFactory.Register("gamekitUISlot", new Handlers.GameKit.GameKitUISlotHandler());
            CommandHandlerFactory.Register("gamekitUISelection", new Handlers.GameKit.GameKitUISelectionHandler());

            // Logic Pillar
            CommandHandlerFactory.Register("gamekitCombat", new Handlers.GameKit.GameKitCombatHandler());

            // Presentation Pillar
            CommandHandlerFactory.Register("gamekitFeedback", new Handlers.GameKit.GameKitFeedbackHandler());
            CommandHandlerFactory.Register("gamekitVFX", new Handlers.GameKit.GameKitVFXHandler());
            CommandHandlerFactory.Register("gamekitAudio", new Handlers.GameKit.GameKitAudioHandler());

            // Graph Analysis handlers
            RegisterGraphAnalysisHandlers();
        }

        /// <summary>
        /// 関係性グラフ解析ツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterGraphAnalysisHandlers()
        {
            CommandHandlerFactory.Register("classDependencyGraph", new Handlers.HighLevel.ClassDependencyGraphHandler());
            CommandHandlerFactory.Register("sceneReferenceGraph", new Handlers.HighLevel.SceneReferenceGraphHandler());
            CommandHandlerFactory.Register("sceneRelationshipGraph", new Handlers.HighLevel.SceneRelationshipGraphHandler());
        }
    }
}

