# McpCommandProcessor.cs リファクタリング計画

**作成日**: 2024年11月25日  
**ステータス**: Phase 1 完了 ✅

---

## 📋 概要

`McpCommandProcessor.cs`（元：9,265行）は、SkillForUnityの中核を成すファイルですが、その巨大さが保守性を低下させています。本ドキュメントは、このファイルを機能別に分割する段階的リファクタリング計画を示します。

### 目標
- **保守性の向上**: 各ファイルを500-1500行に抑え、理解しやすくする
- **テスト容易性**: 機能ごとに独立したテストを可能にする
- **並行開発**: 複数の開発者が異なる機能を同時に編集可能にする
- **コードレビュー**: 変更をレビュー可能なサイズに保つ

### アプローチ
**段階的リファクタリング**を採用し、各フェーズで1つの機能セクションを分割します。各フェーズは独立したPRとしてレビュー・マージされます。

---

## 🗂️ ディレクトリ構造

```
Assets/SkillForUnity/Editor/MCPBridge/
├── McpCommandProcessor.cs          # メインディスパッチャー（現在: 8,129行）
├── Core/
│   └── McpCommandProcessor.Helpers.cs  # ✅ ヘルパーメソッド（Phase 1完了）
├── Scene/
│   └── McpCommandProcessor.Scene.cs    # 🔜 シーン管理（Phase 2）
├── GameObject/
│   └── McpCommandProcessor.GameObject.cs  # 🔜 GameObject操作（Phase 3）
├── Component/
│   └── McpCommandProcessor.Component.cs   # 🔜 コンポーネント管理（Phase 4）
├── Asset/
│   └── McpCommandProcessor.Asset.cs       # 🔜 アセット管理（Phase 5）
│   └── McpCommandProcessor.ScriptableObject.cs  # 🔜 ScriptableObject（Phase 6）
├── UI/
│   └── McpCommandProcessor.UI.cs          # 🔜 UI操作（Phase 7）
├── Prefab/
│   └── McpCommandProcessor.Prefab.cs      # 🔜 Prefab管理（Phase 8）
├── Settings/
│   └── McpCommandProcessor.Settings.cs    # 🔜 プロジェクト設定（Phase 9）
├── Utilities/
│   └── McpCommandProcessor.Utilities.cs   # 🔜 ユーティリティ（Phase 10）
└── Template/
    └── McpCommandProcessor.Template.cs    # 🔜 テンプレート生成（Phase 11）
```

---

## 📊 リファクタリングフェーズ

### ✅ Phase 1: ヘルパーメソッドの抽出（完了）

**日付**: 2024年11月25日  
**コミット**: `b61c643`

#### 実施内容
- `McpCommandProcessor.cs`を`partial class`に変更
- ヘルパーメソッドを`Core/McpCommandProcessor.Helpers.cs`に抽出（約1,100行）

#### 抽出されたメソッド
- **Payload解析**: `GetString`, `GetBool`, `GetInt`, `GetFloat`, `GetList`, `GetStringList`
- **シリアライズ**: `SerializeValue`, `SerializeObjectProperties`
- **解決**: `ResolveType`, `ResolveGameObject`, `ResolveAssetPath`, `ResolveGameObjectFromPayload`, `ResolveAssetPathFromPayload`
- **検証**: `ValidateAssetPath`, `EnsureValue`, `EnsureDirectoryExists`
- **適用**: `ApplyProperty`, `ApplyPropertyToObject`, `ApplyAssetImporterProperties`, `ConvertValue`
- **記述**: `DescribeComponent`, `DescribeAsset`

#### 結果
- **削除行数**: 1,144行
- **ファイルサイズ**: 9,265行 → 8,129行（**12%削減**）
- **コンパイルエラー**: ✅ なし
- **テスト**: ✅ すべて成功

---

### ✅ Phase 2: シーン管理の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 413行

#### 抽出したメソッド
- `HandleSceneManage` - メインディスパッチャー
- **シーン操作**: `CreateScene`, `LoadScene`, `SaveScenes`, `DeleteScene`, `DuplicateScene`, `InspectScene`
- **ビルド設定**: `ListBuildSettings`, `AddToBuildSettings`, `RemoveFromBuildSettings`, `ReorderBuildSettings`, `SetBuildSettingsEnabled`

#### ファイル
- ✅ `Scene/McpCommandProcessor.Scene.cs` (約470行)

#### 効果
- ✅ シーン操作のロジックが独立
- ✅ シーン関連のテストが容易に
- ✅ ビルド設定管理が明確化

---

### ✅ Phase 3: GameObject操作の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 406行

#### 抽出したメソッド
- `HandleGameObjectManage` - メインディスパッチャー
- **基本操作**: `CreateGameObject`, `DeleteGameObject`, `MoveGameObject`, `RenameGameObject`, `UpdateGameObject`, `DuplicateGameObject`, `InspectGameObject`
- **バッチ操作**: `FindMultipleGameObjects`, `DeleteMultipleGameObjects`, `InspectMultipleGameObjects`

#### ファイル
- `GameObject/McpCommandProcessor.GameObject.cs`

#### 結果
- **削除行数**: 401行（406行削除、5行コメント追加）
- **ファイルサイズ**: 7,611行 → 7,210行（**5.3%削減**）
- **コンパイルエラー**: ✅ なし
- **テスト**: ✅ すべて成功

#### 達成した効果
- GameObject操作が独立したファイルに整理
- バッチ処理メソッドの可読性向上
- 単一責任原則の遵守

---

### ✅ Phase 4: コンポーネント管理の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 606行

#### 抽出したメソッド
- `HandleComponentManage` - メインディスパッチャー
- **基本操作**: `AddComponent`, `RemoveComponent`, `UpdateComponent`, `InspectComponent`
- **バッチ操作**: `AddMultipleComponents`, `RemoveMultipleComponents`, `UpdateMultipleComponents`, `InspectMultipleComponents`

#### ファイル
- `Component/McpCommandProcessor.Component.cs`

#### 結果
- **削除行数**: 602行（606行削除、4行コメント追加）
- **ファイルサイズ**: 7,210行 → 6,608行（**8.3%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- コンポーネント操作が独立したファイルに整理
- リフレクションロジックの集約
- プロパティフィルタリング機能の明確化

---

### ✅ Phase 5: アセット管理の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 433行

#### 抽出したメソッド
- `HandleAssetManage` - メインディスパッチャー
- **テキストアセット**: `CreateTextAsset`, `UpdateTextAsset`
- **基本操作**: `UpdateAssetImporter`, `UpdateAsset`, `DeleteAsset`, `RenameAsset`, `DuplicateAsset`, `InspectAsset`
- **バッチ操作**: `FindMultipleAssets`, `DeleteMultipleAssets`, `InspectMultipleAssets`

#### ファイル
- `Asset/McpCommandProcessor.Asset.cs`

#### 結果
- **削除行数**: 428行（433行削除、5行コメント追加）
- **ファイルサイズ**: 6,608行 → 6,180行（**6.5%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- アセット操作の独立性向上
- AssetDatabaseとの連携が明確に
- ImporterプロパティとAssetプロパティの区別が明確化

---

### ✅ Phase 6: ScriptableObject管理の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 479行

#### 抽出したメソッド
- `HandleScriptableObjectManage` - メインディスパッチャー
- **CRUD操作**: `CreateScriptableObject`, `InspectScriptableObject`, `UpdateScriptableObject`, `DeleteScriptableObject`
- **その他操作**: `DuplicateScriptableObject`, `ListScriptableObjects`, `FindScriptableObjectsByType`

#### ファイル
- `Asset/McpCommandProcessor.ScriptableObject.cs`

#### 結果
- **削除行数**: 474行（479行削除、5行コメント追加）
- **ファイルサイズ**: 6,180行 → 5,706行（**7.7%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- ScriptableObject操作が専用ファイルに整理
- GUIDとパス両方によるアセット解決
- 改善されたエラーハンドリングとプロパティ適用

---

### ✅ Phase 7: UI操作の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 2,064行

#### 抽出したメソッド
- `HandleUguiRectAdjust`, `HandleUguiAnchorManage`, `HandleUguiManage`
- `HandleUguiCreateFromTemplate`, `HandleUguiLayoutManage`, `HandleUguiDetectOverlaps`
- UI関連のヘルパーメソッド（`DetectRectOverlap`等）

#### ファイル
- `UI/McpCommandProcessor.UI.cs`

#### 結果
- **削除行数**: 2,058行（2,064行削除、6行コメント追加）
- **ファイルサイズ**: 5,692行 → 3,634行（**36.2%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- UI操作ロジックの完全な集約
- RectTransform、Canvas、Layout管理が一箇所に
- 最大の単一分割フェーズ

---

### ✅ Phase 8: Prefab管理の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 250行

#### 抽出したメソッド
- `HandlePrefabManage` - メインディスパッチャー
- Prefab操作: `CreatePrefab`, `UpdatePrefab`, `InspectPrefab`, `InstantiatePrefab`, `UnpackPrefab`
- オーバーライド管理: `ApplyPrefabOverrides`, `RevertPrefabOverrides`

#### ファイル
- `Prefab/McpCommandProcessor.Prefab.cs`

#### 結果
- **削除行数**: 245行（250行削除、5行コメント追加）
- **ファイルサイズ**: 3,634行 → 3,389行（**6.7%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- Prefabワークフローの独立性向上
- インスタンスとアセットの関係が明確化
- オーバーライド管理の明確化

---

### ✅ Phase 9: プロジェクト設定の分割（完了）

**日付**: 2024年11月27日  
**コミット**: [準備中]  
**対象行数**: 1,666行

#### 抽出したメソッド
- `HandleProjectSettingsManage` - PlayerSettings, Quality, Time, Physics, Audio
- `HandleRenderPipelineManage` - レンダーパイプライン管理
- `HandleTagLayerManage` - タグとレイヤー管理
- `HandleConstantConvert` - 定数変換ユーティリティ
- プロジェクトコンパイル関連メソッド

#### ファイル
- `Settings/McpCommandProcessor.Settings.cs`

#### 結果
- **削除行数**: 1,661行（1,666行削除、5行コメント追加）
- **ファイルサイズ**: 3,389行 → 1,728行（**49.0%削減**）
- **コンパイルエラー**: ✅ なし

#### 達成した効果
- プロジェクト設定操作の完全な集約
- レンダーパイプライン管理の独立
- 定数変換とコンパイル管理の統合

---

### 🔜 Phase 10: ユーティリティの分割（計画中）

**予定日**: 2025年3月  
**対象行数**: 約400行

#### 抽出予定のメソッド
- `HandleContextInspect`
- コンパイル管理関連メソッド
- その他のユーティリティメソッド

#### ファイル
- `Utilities/McpCommandProcessor.Utilities.cs`

#### 期待効果
- ユーティリティ機能の集約
- 共通機能の再利用性向上

---

### 🔜 Phase 11: テンプレート生成の分割（計画中）

**予定日**: 2025年3月  
**対象行数**: 約800行

#### 抽出予定のメソッド
- `HandleSceneQuickSetup`, `HandleGameObjectCreateFromTemplate`
- `HandleDesignPatternGenerate`, `HandleScriptTemplateGenerate`
- `HandleTemplateManage`, `HandleMenuHierarchyCreate`

#### ファイル
- `Template/McpCommandProcessor.Template.cs`

#### 期待効果
- テンプレート生成ロジックの集約
- デザインパターンコードの独立性向上

---

## 📈 期待される効果

### コード品質
- **可読性**: ✅ 各ファイルが特定の責務に集中
- **保守性**: ✅ 変更の影響範囲が明確
- **テスト性**: ✅ 機能ごとの単体テストが容易

### 開発効率
- **並行開発**: ✅ 複数の開発者が同時に作業可能
- **コードレビュー**: ✅ レビュー可能なサイズの変更
- **デバッグ**: ✅ 問題箇所の特定が容易

### パフォーマンス
- **コンパイル時間**: ✅ 変更時の再コンパイル範囲が縮小
- **IDEパフォーマンス**: ✅ ファイル解析が高速化

---

## 🎯 ガイドライン

### 各フェーズの進め方

1. **ブランチ作成**
   ```bash
   git checkout -b refactor/phase-N-description
   ```

2. **ファイル作成**
   - `partial class`として新しいファイルを作成
   - 適切な`#region`を使用

3. **メソッド移動**
   - 元のファイルからメソッドをコピー
   - コメントとドキュメントを保持
   - 依存関係を確認

4. **テスト**
   - コンパイルエラーの確認
   - 既存のテストを実行
   - 必要に応じて新しいテストを追加

5. **レビュー**
   - PRを作成
   - コードレビューを受ける
   - フィードバックに対応

6. **マージ**
   - すべてのチェックが通過
   - レビュー承認後にマージ

### 命名規則

- **ファイル名**: `McpCommandProcessor.[Feature].cs`
- **Region名**: `#region [Feature Name] Operations`
- **メソッド名**: 既存の命名規則を維持

### コミットメッセージ

```
refactor: Phase N - [Feature Name] operations extraction

- Extracted [Method1], [Method2], ... to [File]
- Reduced McpCommandProcessor.cs by ~XXX lines
- All tests passing
- No breaking changes

Part of refactoring plan: docs/REFACTORING_PLAN.md
```

---

## ✅ チェックリスト

各フェーズ完了時に以下を確認：

- [ ] すべてのメソッドが正しく移動された
- [ ] コンパイルエラーがない
- [ ] すべてのテストが成功
- [ ] コードレビューが完了
- [ ] ドキュメントが更新された
- [ ] このドキュメントが更新された

---

## 📝 注意事項

### 互換性
- すべての既存のAPIは変更なし
- Python側のMCPサーバーへの影響なし
- `partial class`により、外部からは単一のクラスとして見える

### リスク軽減
- 各フェーズは独立してロールバック可能
- 段階的なマージにより、問題の早期発見が可能
- 継続的なテストにより、回帰を防止

### ベストプラクティス
- 各ファイルは500-1500行を目安に
- メソッドは論理的にグループ化
- 依存関係を最小化
- ドキュメントを常に最新に保つ

---

## 📊 進捗状況

| Phase | ステータス | 完了日 | 削減行数 | コミット |
|-------|-----------|--------|----------|----------|
| 1. Helpers | ✅ 完了 | 2024-11-25 | 1,144行 | `b61c643` |
| 2. Scene | ✅ 完了 | 2024-11-27 | 413行 | `0ab29cc` |
| 3. GameObject | ✅ 完了 | 2024-11-27 | 401行 | `15c5e28` |
| 4. Component | ✅ 完了 | 2024-11-27 | 602行 | `df140bd` |
| 5. Asset | ✅ 完了 | 2024-11-27 | 428行 | `df140bd` |
| 6. ScriptableObject | ✅ 完了 | 2024-11-27 | 474行 | `df140bd` |
| 7. UI | ✅ 完了 | 2024-11-27 | 2,058行 | [準備中] |
| 8. Prefab | ✅ 完了 | 2024-11-27 | 245行 | [準備中] |
| 9. Settings | ✅ 完了 | 2024-11-27 | 1,661行 | [準備中] |
| 10. Utilities | 🔜 計画中 | 2025-03-XX | 400行 | - |
| 11. Template | 🔜 計画中 | 2025-03-XX | 800行 | - |
| **合計** | **82%完了** | - | **7,426行** | - |

**現在のファイルサイズ**: 1,728行  
**最終目標**: ~1,971行（メインディスパッチャーのみ）  
**削減率**: 約**78%削減予定** → 現在**81.4%削減済み** ✅ **目標達成！**

---

## 🚀 次のステップ

1. **Phase 6のコミット** ← 現在ここ
   - CHANGELOGの更新
   - コミット&プッシュ

2. **Phase 7の準備**
   - UI関連メソッドの特定
   - 依存関係の確認

3. **継続的改善**
   - 各フェーズでの学びを次に活かす
   - ガイドラインの更新
   - チーム内での知見共有

---

**最終更新**: 2024年11月27日  
**次回レビュー**: Phase 7開始時

