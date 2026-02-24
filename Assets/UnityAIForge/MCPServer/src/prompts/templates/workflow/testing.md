# テスト・検証 ワークフローガイド (Unity-AI-Forge v{VERSION})

Logic Pillarツールを活用したPDCAサイクルによるゲーム品質保証ガイドです。シーン整合性チェック・参照グラフ・クラス依存解析・エラーモニタリングを組み合わせて、問題を早期発見・修正します。

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → アルファ → ベータ → リリース
                                        ↑            ↑          ↑        ↑
                                   [テスト・検証: 各フェーズの品質ゲートで実施]
```

本ガイドは**横断的ガイド**です。特定のフェーズに属するのではなく、プロトタイプ以降の全フェーズで品質ゲートとして繰り返し参照してください。各フェーズの品質ゲートセクションから本ガイドへのリンクがあります。

**前提**: テスト対象のシーン・スクリプトが存在すること。プロトタイプ完了時、アルファ完了時、ベータ完了時、リリース前の各マイルストーンで全検証を実施します。

---

## 概要

Unity-AI-ForgeのLogic Pillar（validate_integrity, class_catalog, class_dependency_graph, scene_reference_graph, scene_relationship_graph）は、シーン・コード・参照の健全性を多角的に検証するツール群です。これらをPDCA（Plan-Do-Check-Act）サイクルに組み込むことで、問題の早期発見と効率的な修正が可能になります。

**テスト・検証の原則:**
- **Plan**: 変更前に inspect/graph で現状を把握する
- **Do**: 適切なレイヤーのツールで変更を実行する
- **Check**: validate_integrity + console_log + scene_reference_graph で確認
- **Act**: 問題を修正してから次のステップへ進む
- 各マイルストーン（プロトタイプ完了/本番移行/リリース前）で必ず全検証を実施
- エラーは発生したその場で対処し、蓄積させない

---

## ワークフロー概要

```
変更前の現状確認 (Plan) → 実装 (Do) → 整合性チェック (Check)
→ エラー修正 (Act) → プレイモードテスト → 繰り返し
```

---

## PDCAサイクル詳細

| フェーズ | やること | 使用ツール |
|---------|---------|-----------|
| **Plan** | 変更対象の現状・影響範囲を確認 | `scene_reference_graph(findReferencesTo)`, `class_catalog(inspectType)`, `class_dependency_graph(analyzeClass/findDependents)` |
| **Do** | 適切な順序で変更を実行 | GameKit, Batch, CRUD操作, `compilation_await` |
| **Check** | 変更後の整合性を検証 | `validate_integrity(all)`, `scene_relationship_graph(analyzeAll)`, `console_log(getErrors)`, `scene_reference_graph(findOrphans)` |
| **Act** | 問題を修正してサイクルを閉じる | `validate_integrity(removeMissingScripts)`, `event_wiring(wire)`, `prefab_crud(applyOverrides)` |

---

## 推奨手順

1. **事前確認 (Plan)** - 変更前の参照・依存関係を記録
2. **変更実施 (Do)** - MCPツールで変更を実行
3. **コンパイル確認** - compilation_await + console_log(getCompilationErrors)
4. **整合性チェック** - validate_integrity(all) で全項目確認
5. **参照グラフ確認** - scene_reference_graph で孤立・破損参照を検出
6. **シーン遷移確認** - scene_relationship_graph でシーン間関係を検証
7. **コード解析** - class_dependency_graph でコード健全性を確認
8. **プレイモードテスト** - playmode_control で実際の動作確認
9. **エラー対処 (Act)** - 問題を修正してサイクルを閉じる

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: 変更前の現状確認 (Plan)

```python
# シーンの現状を把握
unity_scene_crud(operation='inspect', includeHierarchy=True)

# 変更対象オブジェクトへの参照を確認（削除・リネーム前に必ず実行）
unity_scene_reference_graph(operation='findReferencesTo',
    objectPath='Player')

# 変更対象スクリプトの依存関係を確認
unity_class_dependency_graph(operation='analyzeClass',
    target='PlayerController')

# 変更対象スクリプトに依存しているクラスを確認
unity_class_dependency_graph(operation='findDependents',
    target='HealthSystem')

# 型の詳細（フィールド・メソッド）を確認
unity_class_catalog(operation='inspectType',
    className='EnemyController',
    includeFields=True,
    includeMethods=True)

# シーン内の全MonoBehaviourを一覧
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts')
```

### Step 2: コンパイル後の即時確認

```python
# スクリプト生成・編集後は必ずコンパイル待機
unity_compilation_await(operation='await', timeoutSeconds=60)

# コンパイルエラーを確認
unity_console_log(operation='getCompilationErrors')
# エラーがあれば修正してから次へ進む

# 全ログのサマリー
unity_console_log(operation='getSummary')

# エラーのみ絞り込み
unity_console_log(operation='getErrors')

# 警告の確認
unity_console_log(operation='getWarnings')
```

### Step 3: シーン整合性チェック (Check)

```python
# 全項目チェック（最も重要）
unity_validate_integrity(operation='all')

# 個別チェック（必要に応じて）
# Missing Script の検出
unity_validate_integrity(operation='missingScripts')

# null 参照フィールドの検出
unity_validate_integrity(operation='nullReferences')

# 壊れた UnityEvent の検出
unity_validate_integrity(operation='brokenEvents')

# 壊れた Prefab インスタンスの検出
unity_validate_integrity(operation='brokenPrefabs')

# Missing Script を自動除去（Undo可能）
unity_validate_integrity(operation='removeMissingScripts')
```

### Step 4: 参照グラフによる問題検出

```python
# シーン全体の参照グラフを解析
unity_scene_reference_graph(operation='analyzeScene')

# 特定オブジェクトへの参照を検索（削除前の安全確認）
unity_scene_reference_graph(operation='findReferencesTo',
    objectPath='GameManager')

# 特定オブジェクトからの参照を検索
unity_scene_reference_graph(operation='findReferencesFrom',
    objectPath='Player')

# 孤立オブジェクト（参照されていないオブジェクト）を検出
unity_scene_reference_graph(operation='findOrphans')

# 特定オブジェクトの解析
unity_scene_reference_graph(operation='analyzeObject',
    objectPath='Player')
```

### Step 5: シーン遷移グラフで設計を検証

```python
# 全シーンの遷移関係を解析
unity_scene_relationship_graph(operation='analyzeAll')

# 特定シーンの詳細解析
unity_scene_relationship_graph(operation='analyzeScene',
    scenePath='Assets/Scenes/Levels/Level01.unity')

# Build Settings に未登録のシーンを検出
unity_scene_relationship_graph(operation='validateBuildSettings')

# 特定シーンへの遷移元を確認
unity_scene_relationship_graph(operation='findTransitionsTo',
    scenePath='Assets/Scenes/Levels/Level01.unity')

# 特定シーンからの遷移先を確認
unity_scene_relationship_graph(operation='findTransitionsFrom',
    scenePath='Assets/Scenes/Boot.unity')
```

### Step 6: クラス依存関係の解析

```python
# クラスの依存ツリーを可視化
unity_class_dependency_graph(operation='analyzeClass',
    target='GameManager')

# 特定クラスに依存しているクラスを逆引き
unity_class_dependency_graph(operation='findDependents',
    target='HealthSystem')

# 特定クラスが依存しているクラスを確認
unity_class_dependency_graph(operation='findDependencies',
    target='PlayerController')

# アセンブリ全体の依存解析
unity_class_dependency_graph(operation='analyzeAssembly',
    target='Assembly-CSharp')

# 名前空間単位の依存解析
unity_class_dependency_graph(operation='analyzeNamespace',
    target='MyGame.Characters')

# クラスカタログで型の一覧を確認
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts')

unity_class_catalog(operation='listTypes',
    typeKind='ScriptableObject',
    searchPath='Assets/Data')

unity_class_catalog(operation='listTypes',
    typeKind='Enum',
    searchPath='Assets/Scripts')
```

### Step 7: プレイモードテスト

```python
# プレイ開始
unity_playmode_control(operation='play')

# プレイ中にログを確認（プレイ中のエラー検出）
unity_console_log(operation='getErrors')
unity_console_log(operation='getRecent')

# フレーム送りでデバッグ
unity_playmode_control(operation='pause')
unity_playmode_control(operation='step')   # 1フレーム進める
unity_playmode_control(operation='step')   # さらに1フレーム

# 再開
unity_playmode_control(operation='unpause')

# 停止
unity_playmode_control(operation='stop')

# 停止後に再確認
unity_console_log(operation='getErrors')
unity_validate_integrity(operation='all')
```

### Step 8: 問題の修正 (Act)

```python
# Missing Script の一括除去
unity_validate_integrity(operation='removeMissingScripts')

# 壊れた UnityEvent の修正（再接続）
unity_event_wiring(operation='wire',
    source={'gameObject': 'Canvas/Button', 'component': 'Button', 'event': 'onClick'},
    target={'gameObject': 'GameManager', 'method': 'StartGame'})

# Prefab の変更を適用（applyOverrides を使用）
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Player')

# Prefab のリバート（意図しない変更を戻す: revertOverrides を使用）
unity_prefab_crud(operation='revertOverrides', gameObjectPath='Enemy')

# コンソールログをクリアして再確認
unity_console_log(operation='clear')
unity_playmode_control(operation='play')
unity_console_log(operation='getErrors')
unity_playmode_control(operation='stop')
```

### Step 9: マイルストーン検証（全チェック一括実行）

```python
# マイルストーン到達時（プロトタイプ完了・本番移行・リリース前）に実施

# 1. コンパイルエラーなし
unity_compilation_await(operation='await', timeoutSeconds=60)
unity_console_log(operation='getCompilationErrors')

# 2. シーン整合性
unity_validate_integrity(operation='all')
unity_validate_integrity(operation='removeMissingScripts')

# 3. 参照健全性
unity_scene_reference_graph(operation='analyzeScene')
unity_scene_reference_graph(operation='findOrphans')

# 4. シーン遷移
unity_scene_relationship_graph(operation='analyzeAll')
unity_scene_relationship_graph(operation='validateBuildSettings')

# 5. コード健全性
unity_class_dependency_graph(operation='analyzeClass',
    target='GameManager')
unity_class_dependency_graph(operation='findDependents',
    target='GameManager')
unity_class_catalog(operation='listTypes', typeKind='MonoBehaviour')

# 6. プレイモード動作確認
unity_playmode_control(operation='play')
unity_console_log(operation='getErrors')
unity_playmode_control(operation='stop')

# 7. 最終エラーサマリー
unity_console_log(operation='getSummary')
```

---

## チェックリスト

### 変更前 (Plan)
- [ ] scene_reference_graph(findReferencesTo) で変更対象の被参照を確認した
- [ ] class_dependency_graph(analyzeClass/findDependents) で依存関係を確認した
- [ ] 削除する場合は参照元を先に解除した

### 変更後 (Check)
- [ ] compilation_await でコンパイル完了を待った
- [ ] console_log(getCompilationErrors) でコンパイルエラーなし確認
- [ ] validate_integrity(all) で整合性エラーなし確認
- [ ] console_log(getErrors) でランタイムエラーなし確認

### 参照検証
- [ ] scene_reference_graph(findOrphans) で孤立オブジェクトなし確認
- [ ] scene_relationship_graph(validateBuildSettings) でビルド設定正常確認

### プレイモード
- [ ] playmode_control(play) で正常動作確認
- [ ] プレイ中に console_log(getErrors) でエラーなし確認
- [ ] 問題があれば修正してから次のステップへ

### マイルストーン
- [ ] プロトタイプ完了時に全検証を実施した
- [ ] 本番移行時に全検証を実施した
- [ ] リリース前に全検証を実施した

---

## よくあるエラーパターンと対処法

### Missing Script エラー

**症状**: `validate_integrity` が Missing Script を報告する

**原因**:
- スクリプトを削除したが、アタッチしたままのGameObjectが残っている
- スクリプトのnamespaceや型名を変更した
- アセンブリ定義を変更してスクリプトが別アセンブリに移動した

**対処**:
```python
# Missing Script を検出して一括除去
unity_validate_integrity(operation='missingScripts')
unity_validate_integrity(operation='removeMissingScripts')

# 除去後に再チェック
unity_validate_integrity(operation='all')
```

### 壊れた UnityEvent

**症状**: `validate_integrity` がブロークンイベントを報告する

**原因**:
- イベントターゲットのGameObjectが削除・リネームされた
- メソッド名を変更した

**対処**:
```python
# イベントの状態を確認
unity_event_wiring(operation='listEvents', gameObjectPath='Canvas/Button')

# 再接続
unity_event_wiring(operation='wire',
    source={'gameObject': 'Canvas/Button', 'component': 'Button', 'event': 'onClick'},
    target={'gameObject': 'GameManager', 'method': 'StartGame'})
```

### null 参照エラー

**症状**: プレイ中に "NullReferenceException" が出る

**原因**:
- SerializeField が未設定
- GetComponent の戻り値がnull
- Start() よりも早く参照にアクセスしている

**対処**:
```python
# null 参照フィールドを検出
unity_validate_integrity(operation='nullReferences')

# コンポーネントのプロパティを確認
unity_component_crud(operation='inspect',
    gameObjectPath='Player',
    componentType='*',
    includeProperties=True)

# 不足しているコンポーネントを追加
unity_component_crud(operation='add',
    gameObjectPath='Player',
    componentType='Rigidbody2D')
```

### 依存関係の問題

**症状**: クラスの変更が予期しない箇所に影響する

**対処**:
```python
# 影響を受けるクラスを確認
unity_class_dependency_graph(operation='findDependents',
    target='HealthSystem')

# 詳細な依存関係を解析
unity_class_dependency_graph(operation='analyzeClass',
    target='HealthSystem')

# 依存先を確認
unity_class_dependency_graph(operation='findDependencies',
    target='PlayerController')

# インターフェースや ScriptableObject で依存を逆転させる
# (コード修正が必要)
```

### Build Settings 未登録シーン

**症状**: `SceneManager.LoadScene` 実行時に "Scene not found" エラー

**対処**:
```python
# Build Settings の状態を確認
unity_scene_relationship_graph(operation='validateBuildSettings')

# 現在の登録一覧を確認
unity_projectSettings_crud(operation='listBuildScenes')

# 未登録シーンを追加
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Level02.unity')

# 再確認
unity_scene_relationship_graph(operation='validateBuildSettings')
```

### コンパイルエラー後のコンポーネント参照破損

**症状**: コンパイルエラー修正後も Missing Script が残る

**対処**:
```python
# コンパイル完了を確認してから操作
unity_compilation_await(operation='await', timeoutSeconds=60)
unity_console_log(operation='getCompilationErrors')  # エラーゼロを確認

# Missing Script を除去
unity_validate_integrity(operation='removeMissingScripts')

# コンポーネントを再アタッチ
unity_component_crud(operation='add',
    gameObjectPath='Player',
    componentType='PlayerController')
```

---

## 注意点・落とし穴

**validate_integrity の操作名に注意**
正しい操作名は `missingScripts`, `nullReferences`, `brokenEvents`, `brokenPrefabs`, `removeMissingScripts`, `all` です。checkMissingScripts や checkNullReferences 等は存在しません。

**validate_integrity はプレイモード中に実行しない**
プレイモード中は一時オブジェクトが存在するためfalse positiveが出ます。必ずstop後に実行してください。

**scene_reference_graph はアクティブシーンのみ対象**
複数シーンを Additive でロードしている場合、全シーンが対象です。単一シーンのみ検証したい場合は対象シーンのみロードしてから実行してください。

**class_dependency_graph に detectCycles / analyzeAll 操作はない**
使用可能な操作は analyzeClass, analyzeAssembly, analyzeNamespace, findDependents, findDependencies です。循環依存の検出には analyzeClass で個別に確認してください。

**scene_relationship_graph に findOrphanScenes 操作はない**
使用可能な操作は analyzeAll, analyzeScene, findTransitionsTo, findTransitionsFrom, validateBuildSettings です。

**prefab_crud の操作名に注意**
apply ではなく applyOverrides、revert ではなく revertOverrides を使用してください。

**console_log(getRecent) はプレイモード中のログを含む**
プレイモード停止後も直前のプレイセッションのログが残ります。clear して再プレイすると純粋な結果が得られます。

**compilation_await のタイムアウト**
大規模プロジェクトではコンパイルに時間がかかります。timeoutSeconds=60 以上を推奨します。

**removeMissingScripts は Undo 可能**
誤って実行してもCtrl+Zで元に戻せます。ただし、保存(scene_crud save)後はUndoできません。

---

## 関連ツール一覧

| ツール | テスト・検証での用途 |
|--------|-------------------|
| `unity_validate_integrity` | missingScripts / nullReferences / brokenEvents / brokenPrefabs / all / removeMissingScripts |
| `unity_scene_reference_graph` | analyzeScene / analyzeObject / findReferencesTo / findReferencesFrom / findOrphans |
| `unity_scene_relationship_graph` | analyzeAll / analyzeScene / findTransitionsTo / findTransitionsFrom / validateBuildSettings |
| `unity_class_dependency_graph` | analyzeClass / analyzeAssembly / analyzeNamespace / findDependents / findDependencies |
| `unity_class_catalog` | listTypes / inspectType |
| `unity_console_log` | getRecent / getErrors / getWarnings / getLogs / clear / getCompilationErrors / getSummary |
| `unity_playmode_control` | play / pause / unpause / stop / step / getState |
| `unity_compilation_await` | await / status |
| `unity_scene_crud` | inspect / save |
| `unity_component_crud` | inspect / add で確認・再アタッチ |
| `unity_event_wiring` | wire / listEvents / clearEvent で再接続 |
| `unity_prefab_crud` | applyOverrides / revertOverrides |
| `unity_projectSettings_crud` | addSceneToBuild / listBuildScenes でBuild Settings修正 |
