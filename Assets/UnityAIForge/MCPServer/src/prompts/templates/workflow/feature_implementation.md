# 機能実装ワークフローガイド {VERSION}

## 概要

複数システムにまたがる機能を効率的に実装するためのワークフローガイド。
データ層→ロジック層→UI層の実装順序、コンパイル待ちの最小化、クロスシーン参照の管理方法を提供する。

## パイプライン位置

```
企画書/ロードマップ → [本ガイド] → 実装 → テスト → リリース
```

設計フェーズ（`game_workflow_guide(phase='design')`）の後、プロトタイプまたはアルファフェーズで使用。

## 実装順序の原則

### Step 1: データ層（ScriptableObject）を先に定義

**なぜ先か**: SO はシーンに依存しない。コンパイル後に複数シーンから参照できる。

```python
# EventChannel を一括生成（createMultiple で1回のコンパイル待ちに集約）
unity_gamekit_data(dataType='eventChannel', operation='createMultiple', items=[
    {'dataId': 'OnPlayerDeath', 'eventType': 'void', 'assetPath': 'Assets/Data/Events/OnPlayerDeath.asset', 'autoCreateAsset': True},
    {'dataId': 'OnDamage', 'eventType': 'float', 'assetPath': 'Assets/Data/Events/OnDamage.asset', 'autoCreateAsset': True},
    {'dataId': 'OnScoreChanged', 'eventType': 'int', 'assetPath': 'Assets/Data/Events/OnScoreChanged.asset', 'autoCreateAsset': True}
])
unity_compilation_await(operation='await')
# → autoCreateAsset により、コンパイル完了時に .asset も自動生成される

# DataContainer も一括生成
unity_gamekit_data(dataType='dataContainer', operation='createMultiple', items=[
    {'dataId': 'PlayerStats', 'fields': [
        {'name': 'health', 'fieldType': 'int', 'defaultValue': 100},
        {'name': 'score', 'fieldType': 'int', 'defaultValue': 0}
    ], 'resetOnPlay': True, 'assetPath': 'Assets/Data/PlayerStats.asset', 'autoCreateAsset': True},
    {'dataId': 'SelectedLevel', 'fields': [
        {'name': 'levelIndex', 'fieldType': 'int', 'defaultValue': 0}
    ], 'resetOnPlay': False, 'assetPath': 'Assets/Data/SelectedLevel.asset', 'autoCreateAsset': True}
])
unity_compilation_await(operation='await')
```

### Step 2: ロジック層（C#スクリプト）

```python
# ビジネスロジックを実装
# asset_crud の .cs 操作は自動的にコンパイル待ちが入る
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Systems/ScoreManager.cs', content='...')
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Systems/DamageSystem.cs', content='...')
```

### Step 3: UI層

```python
# UIバインディングを一括生成
unity_gamekit_ui(widgetType='binding', operation='createMultiple', bindings=[
    {'targetPath': 'Canvas/HUD', 'bindingId': 'hp_bar', 'sourceType': 'custom', 'format': 'percent'},
    {'targetPath': 'Canvas/HUD', 'bindingId': 'score', 'sourceType': 'custom', 'format': 'raw'}
])
unity_compilation_await(operation='await')
```

### Step 4: クロスシーン参照設定

```python
# crossSceneUpdate で複数シーンの参照を一括設定
unity_component_crud(operation='crossSceneUpdate', updates=[
    {'scenePath': 'Assets/Scenes/GameScene.unity', 'gameObjectPath': 'GameManager',
     'componentType': 'ScoreManager', 'propertyChanges': {
         'onScoreChanged': {'$ref': 'Assets/Data/Events/OnScoreChanged.asset'},
         'playerStats': {'$ref': 'Assets/Data/PlayerStats.asset'}
     }},
    {'scenePath': 'Assets/Scenes/ResultScene.unity', 'gameObjectPath': 'ResultUI',
     'componentType': 'ResultDisplay', 'propertyChanges': {
         'playerStats': {'$ref': 'Assets/Data/PlayerStats.asset'}
     }}
])
```

### Step 5: 検証

```python
# 全シーンの整合性チェック
unity_validate_integrity(operation='report', scope='build_scenes')
```

## コンパイル待ち最適化パターン

| パターン | コンパイル回数 | 方法 |
|---------|-------------|------|
| 個別create × N | N回 | 各createの後にawait |
| **createMultiple** | **1回** | 同じdataTypeをまとめて生成 |
| **batch_sequential** | **自動** | 連続createをまとめ、最後にのみawait |
| asset_crud (.cs) | 自動1回 | 自動await付き |

## データフロー設計チェックリスト

- [ ] シーン間で共有するデータ → **DataContainer** (SO)
- [ ] システム間の通知 → **EventChannel** (SO)
- [ ] ランタイムのオブジェクト追跡 → **RuntimeSet** (SO, OnChanged イベント付き)
- [ ] 頻繁な生成/破棄 → **ObjectPool**
- [ ] UI表示の更新 → **UIBinding** (SetValue push API)
- [ ] 画面モード切替 → **UIState** (defineState + applyState)

## 注意点・落とし穴

1. **PlayerPrefs を使わない**: シーン間データは DataContainer (SO) で渡す
2. **static クラスを避ける**: SO ベースならインスペクタで確認・デバッグ可能
3. **RuntimeSet は要素型に注意**: デフォルトは GameObject。Serializable クラスには OnChanged イベントで通知パターンを使う
4. **autoCreateAsset**: assetPath を指定しないと機能しない
5. **crossSceneUpdate**: 現在のシーンは自動保存→復帰される。未保存の変更がある場合は先に保存すること
6. **createMultiple は同一 dataType のみ**: 異なる dataType を混ぜたい場合は複数回呼ぶ

## 関連ツール

| ツール | 用途 |
|-------|------|
| `unity_gamekit_data(createMultiple)` | SO スクリプト一括生成 |
| `unity_gamekit_ui(binding, createMultiple)` | UIバインディング一括生成 |
| `unity_component_crud(crossSceneUpdate)` | クロスシーン参照設定 |
| `unity_validate_integrity(report)` | 整合性検証 |
| `unity_scene_reference_graph(findReferencesTo)` | 被参照確認 |
| `unity_class_dependency_graph(analyzeClass)` | クラス依存関係 |
