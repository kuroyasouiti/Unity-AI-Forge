# リリース ワークフローガイド (Unity-AI-Forge v{VERSION})

最終検証とビルド準備。ベータで品質を達成したゲームに対して、包括的な検証・Build Settings最終化・リリースチェックリストを実施するためのガイドです。

---

## 概要

リリースフェーズの目的は「出荷可能な状態を確認し、ビルドを準備する」ことです。新しい機能や演出を追加するフェーズではありません。ベータで完成したゲームに対して、すべての検証ツールを実行し、問題がないことを確認してからビルドを作成します。

**リリースフェーズの原則:**
- 新機能・新演出は追加しない。検証と修正のみ
- Logic Pillar ツールで全項目を検証する
- Build Settings の最終確認を行う
- Project Settings の公開設定を確認する
- 問題が見つかったらベータに戻して修正する

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → アルファ → ベータ → [リリース]
```

**前提**: ベータで演出・品質が完成済み（`game_workflow_guide(phase='beta')`）。全ゲーム機能・演出・UIが統合済みの状態。

---

## ワークフロー概要

```
コンパイル検証 → シーン整合性 → 参照グラフ検証
→ コード健全性 → Build Settings最終化 → Project Settings確認
→ プレイモード最終テスト → ビルド作成
```

---

## 推奨手順

1. **コンパイル検証** - エラー・警告ゼロを確認
2. **シーン整合性検証** - validate_integrity で全項目チェック
3. **シーン遷移グラフ検証** - 全シーンの遷移関係を確認
4. **シーン参照グラフ検証** - 孤立オブジェクト・破損参照を確認
5. **コード健全性検証** - class_dependency_graph で依存関係確認
6. **Build Settings 最終化** - 全シーンの登録・順序を確認
7. **Project Settings 確認** - 公開に必要な設定を確認
8. **プレイモード最終テスト** - 全シーンの動作確認
9. **リリースチェックリスト** - 全項目の最終確認

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: コンパイル検証

```python
# コンパイル完了を確認
unity_compilation_await(operation='await', timeoutSeconds=60)

# コンパイルエラーがゼロであることを確認
unity_console_log(operation='getCompilationErrors')

# 警告も確認（致命的でなくてもログに残る）
unity_console_log(operation='getWarnings')

# コンソールをクリアしてクリーンな状態にする
unity_console_log(operation='clear')
```

### Step 2: シーン整合性検証

全シーンに対して整合性チェックを実行します。

```python
# 全項目チェック
unity_validate_integrity(operation='all')

# Missing Script があれば除去（Undo可能）
unity_validate_integrity(operation='removeMissingScripts')

# 個別チェックで詳細確認
unity_validate_integrity(operation='nullReferences')
unity_validate_integrity(operation='brokenEvents')
unity_validate_integrity(operation='brokenPrefabs')
```

### Step 3: シーン遷移グラフ検証

```python
# 全シーンの遷移関係を解析
unity_scene_relationship_graph(operation='analyzeAll')

# Build Settings に未登録のシーンがないか確認
unity_scene_relationship_graph(operation='validateBuildSettings')

# Boot からの遷移パスを確認
unity_scene_relationship_graph(operation='findTransitionsFrom',
    scenePath='Assets/Scenes/Boot.unity')

# 全レベルシーンへの遷移元を確認
unity_scene_relationship_graph(operation='findTransitionsTo',
    scenePath='Assets/Scenes/Levels/Level01.unity')
```

### Step 4: シーン参照グラフ検証

```python
# シーン全体の参照グラフを解析
unity_scene_reference_graph(operation='analyzeScene')

# 孤立オブジェクト（どこからも参照されていない）を検出
unity_scene_reference_graph(operation='findOrphans')

# 重要オブジェクトへの参照を確認
unity_scene_reference_graph(operation='findReferencesTo',
    objectPath='GameManager')
```

### Step 5: コード健全性検証

```python
# 主要クラスの依存関係を確認
unity_class_dependency_graph(operation='analyzeClass',
    target='GameManager')

# 逆引き: GameManagerに依存するクラスを確認
unity_class_dependency_graph(operation='findDependents',
    target='GameManager')

# 全MonoBehaviourの一覧
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts')

# 全ScriptableObjectの一覧
unity_class_catalog(operation='listTypes',
    typeKind='ScriptableObject',
    searchPath='Assets/Scripts')

# 不要なクラスが残っていないか確認
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts/Proto')  # プロトタイプ用が残っていないか
```

### Step 6: Build Settings 最終化

```python
# 現在のBuild Settingsに登録されたシーン一覧を確認
unity_projectSettings_crud(operation='listBuildScenes')

# Boot が Index 0 であることを確認
# 必要なら並び替え（fromIndex → toIndex で1シーンずつ移動）
unity_projectSettings_crud(operation='reorderBuildScenes',
    fromIndex=3, toIndex=0)  # Boot のインデックスを 0 に移動

# Build Settings のバリデーション
unity_scene_relationship_graph(operation='validateBuildSettings')
```

### Step 7: Project Settings 確認

```python
# Player Settings の確認
unity_projectSettings_crud(operation='read', category='player')

# 必要に応じて更新
unity_projectSettings_crud(operation='write', category='player',
    property='companyName', value='MyStudio')

unity_projectSettings_crud(operation='write', category='player',
    property='productName', value='MyGame')

unity_projectSettings_crud(operation='write', category='player',
    property='bundleVersion', value='1.0.0')

# Quality Settings の確認
unity_projectSettings_crud(operation='read', category='quality')

# Physics Settings の確認
unity_projectSettings_crud(operation='read', category='physics')
unity_projectSettings_crud(operation='read', category='physics2d')

# タグ・レイヤーの最終確認
unity_projectSettings_crud(operation='read', category='tagsLayers')
```

### Step 8: プレイモード最終テスト

```python
# Boot シーンからの完全フロー確認
unity_scene_crud(operation='load',
    scenePath='Assets/Scenes/Boot.unity', additive=False)

# プレイ開始
unity_playmode_control(operation='play')

# エラー確認
unity_console_log(operation='getErrors')
unity_console_log(operation='getWarnings')

# サマリー
unity_console_log(operation='getSummary')

# 停止
unity_playmode_control(operation='stop')

# 最終整合性チェック
unity_validate_integrity(operation='all')
```

---

## リリースチェックリスト

### コンパイル
- [ ] compilation_await でコンパイル完了確認
- [ ] console_log(getCompilationErrors) でコンパイルエラーゼロ
- [ ] console_log(getWarnings) で致命的な警告なし

### シーン整合性
- [ ] validate_integrity(all) でエラーゼロ
- [ ] validate_integrity(nullReferences) で null 参照なし
- [ ] validate_integrity(brokenEvents) で壊れたイベントなし
- [ ] validate_integrity(brokenPrefabs) で壊れたPrefabなし

### シーン遷移
- [ ] scene_relationship_graph(analyzeAll) で全遷移が正常
- [ ] scene_relationship_graph(validateBuildSettings) で未登録シーンなし
- [ ] Boot シーンから全ゲームフローが到達可能

### シーン参照
- [ ] scene_reference_graph(findOrphans) で不要な孤立オブジェクトなし
- [ ] 主要オブジェクト（GameManager等）への参照が正常

### コード健全性
- [ ] class_dependency_graph で循環依存なし
- [ ] class_catalog で不要なスクリプト（Proto/Test等）が残っていない
- [ ] 全ScriptableObjectが正しく参照されている

### Build Settings
- [ ] 全シーンが Build Settings に登録されている
- [ ] Boot シーンが Index 0
- [ ] シーン順序が正しい

### Project Settings
- [ ] companyName / productName / bundleVersion が正しい
- [ ] タグ・レイヤーが設計通り
- [ ] 物理レイヤーマトリクスが正しい
- [ ] Quality Settings がターゲットプラットフォームに適切

### プレイモード
- [ ] Boot シーンからの完全フロー動作確認
- [ ] 全レベルのプレイスルー確認
- [ ] ゲームオーバー・リスタート・タイトル戻りの動作確認
- [ ] console_log(getErrors) でランタイムエラーゼロ

### 最終確認
- [ ] プロジェクト内に不要なテストファイルが残っていない
- [ ] 全Prefabの overrides が apply 済み
- [ ] シーンの保存忘れがない

---

## 問題が見つかった場合

リリースチェックで問題が見つかった場合は、問題の性質に応じて適切なフェーズに戻ります:

| 問題の種類 | 戻り先 |
|-----------|--------|
| ロジックバグ | アルファ (`game_workflow_guide(phase='alpha')`) |
| 演出・ビジュアルの問題 | ベータ (`game_workflow_guide(phase='beta')`) |
| 設計の根本的問題 | 設計 (`game_workflow_guide(phase='design')`) |

詳細なテスト・検証手法は `game_workflow_guide(phase='testing')` を参照してください。

---

## 注意点・落とし穴

**リリースフェーズでは新機能を追加しない**
問題を見つけたら修正のみ。新機能の追加はアルファ/ベータに戻って行ってください。

**validate_integrity はプレイモード中に実行しない**
プレイモード停止後に実行してください。

**Build Settings の Index 0 は必ず Boot にする**
ビルドは Index 0 のシーンから起動します。

**scene_relationship_graph はエディタ内シーンのみ解析**
実行時の動的ロードはグラフに反映されません。遷移スクリプト内の文字列と実際のシーンパスを手動で確認してください。

**bundleVersion はセマンティックバージョニングで管理**
`1.0.0` の形式で管理し、リリースごとに更新してください。

---

## 関連ツール一覧

| ツール | リリースでの用途 |
|--------|----------------|
| `unity_validate_integrity` | all / missingScripts / nullReferences / brokenEvents / brokenPrefabs |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings / findTransitionsTo / findTransitionsFrom |
| `unity_scene_reference_graph` | analyzeScene / findOrphans / findReferencesTo |
| `unity_class_dependency_graph` | analyzeClass / findDependents |
| `unity_class_catalog` | listTypes / inspectType |
| `unity_console_log` | getCompilationErrors / getErrors / getWarnings / getSummary |
| `unity_compilation_await` | await |
| `unity_projectSettings_crud` | read / write / listBuildScenes / reorderBuildScenes |
| `unity_scene_crud` | load / inspect |
| `unity_playmode_control` | play / stop |
| `unity_prefab_crud` | applyOverrides |
