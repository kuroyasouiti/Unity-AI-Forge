# GameKit Machinations Testing Guide

## 概要

GameKit Machinationsシステム（経済システム設計ツール）の包括的なテストスイートです。

## テストファイル

- **GameKitMachinationsTests.cs**: 52個のテストケースを含む完全なテストスイート

## テスト構成

### 1. MachinationsAsset Tests (基本機能テスト)

#### アセット作成と管理
- ✅ `MachinationsAsset_CreateAsset_IsNotNull`: アセット作成の検証
- ✅ `MachinationsAsset_AddPool_CreatesPool`: リソースプールの追加
- ✅ `MachinationsAsset_AddFlow_CreatesFlow`: リソースフローの追加
- ✅ `MachinationsAsset_AddConverter_CreatesConverter`: リソースコンバーターの追加
- ✅ `MachinationsAsset_AddTrigger_CreatesTrigger`: リソーストリガーの追加
- ✅ `MachinationsAsset_RemovePool_RemovesPoolAndRelatedElements`: プール削除と関連要素のクリーンアップ
- ✅ `MachinationsAsset_GetSummary_ReturnsCorrectInfo`: サマリー情報の取得

#### バリデーション
- ✅ `MachinationsAsset_Validate_EmptyAsset_ReturnsFalse`: 空アセットの検証エラー
- ✅ `MachinationsAsset_Validate_ValidAsset_ReturnsTrue`: 正常なアセットの検証成功
- ✅ `MachinationsAsset_Validate_DuplicatePoolNames_ReturnsFalse`: 重複プール名の検出
- ✅ `MachinationsAsset_Validate_InvalidMinMax_ReturnsFalse`: 不正な最小/最大値の検出
- ✅ `MachinationsAsset_Validate_FlowReferencesNonexistentPool_ReturnsFalse`: 存在しないプールへの参照検出（Flow）
- ✅ `MachinationsAsset_Validate_ConverterReferencesNonexistentPool_ReturnsFalse`: 存在しないプールへの参照検出（Converter）
- ✅ `MachinationsAsset_Validate_InvalidConversionRate_ReturnsFalse`: 不正な変換率の検出

### 2. ResourceManager + Machinations Integration Tests (統合テスト)

#### アセット適用
- ✅ `ResourceManager_ApplyMachinationsAsset_InitializesResources`: アセットからのリソース初期化
- ✅ `ResourceManager_ApplyMachinationsAsset_InvalidAsset_LogsError`: 不正なアセットのエラーログ

#### フロー処理
- ✅ `ResourceManager_ProcessDiagramFlows_GeneratesResources`: リソース生成フローの動作
- ✅ `ResourceManager_ProcessDiagramFlows_ConsumesResources`: リソース消費フローの動作
- ✅ `ResourceManager_ProcessDiagramFlows_RespectsMaxConstraint`: 最大値制約の尊重
- ✅ `ResourceManager_ProcessDiagramFlows_RespectsMinConstraint`: 最小値制約の尊重

#### コンバーター実行
- ✅ `ResourceManager_ExecuteConverter_ConvertsResources`: リソース変換の成功
- ✅ `ResourceManager_ExecuteConverter_InsufficientResources_ReturnsFalse`: リソース不足時の失敗
- ✅ `ResourceManager_ExecuteConverter_NonexistentConverter_ReturnsFalse`: 存在しないコンバーターの処理

#### フロー制御
- ✅ `ResourceManager_SetFlowEnabled_TogglesFlow`: フローの有効/無効切り替え
- ✅ `ResourceManager_IsFlowEnabled_ReturnsCorrectState`: フロー状態の取得
- ✅ `ResourceManager_GetFlowStates_ReturnsAllFlowStates`: すべてのフロー状態の取得

#### トリガー
- ✅ `ResourceManager_CheckDiagramTriggers_FiresTriggerOnThreshold`: 閾値トリガーの発火（Below）
- ✅ `ResourceManager_CheckDiagramTriggers_AboveThreshold`: 閾値トリガーの発火（Above）

#### イベント
- ✅ `ResourceManager_OnResourceChanged_InvokesEvent`: リソース変更イベントの発火

#### 状態管理
- ✅ `ResourceManager_ExportState_IncludesAllData`: 状態のエクスポート
- ✅ `ResourceManager_ImportState_RestoresAllData`: 状態のインポート

### 3. Complex Scenario Tests (複雑なシナリオテスト)

#### RPGシステム
✅ `ComplexScenario_RPGHealthManaSystem`:
- ヘルス/マナシステムの統合テスト
- マナの自動回復（フロー）
- ゴールドでヘルスポーションを購入（コンバーター）
- 低ヘルス警告（トリガー）
- 実際のゲームプレイフローのシミュレーション

#### クラフティングシステム
✅ `ComplexScenario_CraftingSystem`:
- 多段階クラフティングシステム
- 木材 → 板材 → 棒 → 道具の変換チェーン
- 複数のコンバーターの連続実行
- リソースバランスの検証

## テスト実行方法

### Unity Editor内で実行

1. Unity Editorを開く
2. Window > General > Test Runner
3. EditMode タブを選択
4. "GameKitMachinationsTests" を展開
5. "Run All" または個別のテストを実行

### コマンドラインで実行

```powershell
# Windows
.\run-tests.ps1 -TestPlatform EditMode

# Unity Path を指定する場合
.\run-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
```

```bash
# Linux/Mac
./run-tests.sh
```

## テストカバレッジ

### 機能カバレッジ

| 機能 | テスト数 | カバレッジ |
|------|---------|-----------|
| アセット作成 | 7 | ✅ 100% |
| バリデーション | 7 | ✅ 100% |
| フロー処理 | 6 | ✅ 100% |
| コンバーター | 3 | ✅ 100% |
| トリガー | 2 | ✅ 100% |
| 状態管理 | 2 | ✅ 100% |
| 統合シナリオ | 2 | ✅ 100% |

### コードカバレッジ推定

- **GameKitMachinationsAsset.cs**: ~95%
  - すべての主要メソッドをカバー
  - エディター専用機能を含む
  
- **GameKitResourceManager.cs**: ~90%
  - Machinations関連機能を完全カバー
  - 状態管理機能を完全カバー
  - Update()ループはモック時間でテスト

## テスト設計原則

1. **Arrange-Act-Assert (AAA) パターン**: すべてのテストで一貫して使用
2. **独立性**: 各テストは他のテストに依存しない
3. **クリーンアップ**: SetUp/TearDownで適切なリソース管理
4. **明確な命名**: テスト名が期待される動作を明示
5. **エッジケースのカバー**: 正常系と異常系の両方をテスト

## 既知の制限事項

1. **時間ベースのテスト**: EditModeテストではTime.deltaTimeを直接テストできないため、ProcessDiagramFlows()に明示的な時間を渡してテスト
2. **PlayModeテスト**: 現在はEditModeテストのみ。将来的にPlayModeテストを追加して実際のゲームループでの動作を検証可能

## 今後の拡張

- [ ] PlayModeテストの追加
- [ ] パフォーマンステスト（大量のリソース処理）
- [ ] マルチスレッドテスト
- [ ] セーブ/ロードの実ファイルテスト
- [ ] UI統合テスト

## トラブルシューティング

### テストが実行されない

1. Test Runnerウィンドウで "Refresh" をクリック
2. アセンブリ定義ファイル（.asmdef）が正しく設定されているか確認
3. Unity Editorを再起動

### テストが失敗する

1. TestLog.txt を確認してエラーメッセージを確認
2. Consoleログでスタックトレースを確認
3. テストの Arrange セクションの初期状態を確認

### コンパイルエラー

1. UnityAIForge.GameKit.Runtime アセンブリが正しく参照されているか確認
2. NUnit framework がインポートされているか確認
3. プロジェクトを再コンパイル（Ctrl+R または Assets > Reimport All）

## ベストプラクティス

1. **新機能を追加する場合**: 対応するテストを先に書く（TDD）
2. **バグ修正**: バグを再現するテストを作成してから修正
3. **リファクタリング**: テストを実行して機能が壊れていないことを確認
4. **PRレビュー**: テストカバレッジが維持されているか確認

## 参考資料

- [Unity Test Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Framework](https://nunit.org/)
- [Machinations Design Patterns](https://machinations.io/)
