# Unity-AI-Forge MCP Server v{VERSION} - Quick Reference

AI駆動型Unity開発ツールキット。MCPサーバー + GameKitフレームワーク。3層構造（Low/Mid/High-Level）+ 3-Pillar GameKit（UI, Presentation, Logic）で効率的な開発を実現。

## 🔴 Critical Rules (必ず守る)

1. **.metaファイルは絶対に編集しない**（Unity自動管理、手動編集は参照破壊）
2. **全Unity操作にMCPツール（unity_*）を使用**
3. **変更前に inspect 操作で対象を確認**
4. **ツール優先順位: High-Level → Mid-Level → Low-Level** の順で選択
5. **コンパイルが必要な操作は自動待機**（ブリッジ再接続で解除）
6. **UI優先設計**: 人間が操作・確認できるUIから優先的に実装する
7. **シーン分割**: 機能ごとにシーンを分ける
8. **PDCAサイクルを遵守**: Plan→Do→Check→Actの順で開発を進める（詳細は「PDCAワークフロー」セクション参照）

---

## 📋 ツール一覧 (49ツール)

### High-Level GameKit (15ツール) - 3-Pillar Architecture

**Logic Pillar - 解析・整合性検証 (5):**
unity_validate_integrity, unity_class_dependency_graph, unity_class_catalog, unity_scene_reference_graph, unity_scene_relationship_graph

**UI Pillar - UIシステム構築 (5):**
unity_gamekit_ui_command, unity_gamekit_ui_binding, unity_gamekit_ui_list, unity_gamekit_ui_slot, unity_gamekit_ui_selection

**Presentation Pillar - 演出・エフェクト (5):**
unity_gamekit_animation_sync, unity_gamekit_effect, unity_gamekit_feedback, unity_gamekit_vfx, unity_gamekit_audio

### Mid-Level Batch (23ツール) - バッチ操作・プリセット・開発支援

**Transform/Layout:** unity_transform_batch, unity_rectTransform_batch
**Physics:** unity_physics_bundle
**Camera:** unity_camera_rig
**UI (UGUI):** unity_ui_foundation, unity_ui_hierarchy, unity_ui_state, unity_ui_navigation
**UI Toolkit:** unity_uitk_document, unity_uitk_asset
**Audio:** unity_audio_source_bundle
**Input:** unity_input_profile
**Character:** unity_character_controller_bundle
**2D:** unity_tilemap_bundle, unity_sprite2d_bundle, unity_animation2d_bundle
**3D/Visual:** unity_material_bundle, unity_light_bundle, unity_particle_bundle, unity_animation3d_bundle
**Events:** unity_event_wiring
**Dev-Cycle:** unity_playmode_control, unity_console_log

### Low-Level CRUD (8ツール) - 基本操作

unity_scene_crud, unity_gameobject_crud, unity_component_crud, unity_asset_crud, unity_scriptableObject_crud, unity_prefab_crud, unity_vector_sprite_convert, unity_projectSettings_crud

### Utility (2ツール) - 接続・コンパイル

unity_ping, unity_compilation_await

### Batch Operations (1ツール)

unity_batch_sequential_execute

---

## 🎯 UI優先設計原則 (Human-First UI Design)

ゲーム開発では、人間が操作・デバッグできるUIを最初に作成することで、開発効率と品質が大幅に向上します。

### なぜUI優先か？

1. **即座のフィードバック**: UIがあればゲームの状態を視覚的に確認できる
2. **手動テスト**: AIが作成したロジックを人間が手動でテスト可能
3. **デバッグ容易**: 問題発生時にUIからゲーム状態を確認・操作できる
4. **イテレーション高速化**: パラメータ調整をUI経由でリアルタイムに行える

### 推奨実装順序

```
1. Canvas/UI構造 → unity_ui_foundation / unity_uitk_asset + unity_uitk_document
2. デバッグUI（ステータス表示、ログ表示）→ unity_ui_hierarchy
3. 操作UI（ボタン、スライダー）→ unity_gamekit_ui_command
4. データバインディング → unity_gamekit_ui_binding
5. ゲームロジック → C#スクリプト (unity_asset_crud)
6. イベント接続 → unity_event_wiring
```

### UI優先の実装例

```python
# ❌ 悪い例: ロジックを先に作り、UIは後回し
# 1. スクリプト作成 → 2. ゲームロジック → 3. UI作成（最後）

# ✅ 良い例: UIを先に作り、ロジックは後
# 1. Canvas作成
unity_ui_foundation(operation='createCanvas', name='GameUI')

# 2. ステータス表示UI
unity_ui_foundation(operation='createText', parentPath='GameUI', name='HPText', text='HP: 100/100')

# 3. 操作ボタンUI（GameKitUICommand で UXML/USS を自動生成）
unity_gamekit_ui_command(
    operation='createCommandPanel',
    panelId='ActionPanel',
    canvasPath='GameUI',
    commands=[
        {'name': 'Attack', 'commandType': 'action', 'label': '攻撃'},
        {'name': 'Heal', 'commandType': 'action', 'label': '回復'},
    ],
    targetType='actor',
    targetActorId='player'
)

# 4. その後でゲームロジック実装（C#スクリプト作成）
unity_asset_crud(
    operation='create',
    assetPath='Assets/Scripts/PlayerController.cs',
    content='...'
)
unity_compilation_await(operation='await', timeoutSeconds=60)
```

---

## 🎬 シーン分割原則 (Scene Separation)

### 推奨シーン構成

```
Assets/Scenes/
├── Boot.unity           # 初期化、マネージャー生成
├── Title.unity          # タイトル画面
├── MainMenu.unity       # メインメニュー
├── Loading.unity        # ローディング画面（Additive）
├── GameUI.unity         # ゲームUI（Additive、複数レベルで共有）
├── AudioManager.unity   # オーディオ管理（Additive、DontDestroyOnLoad）
├── Levels/
│   ├── Level1.unity     # ゲームプレイシーン
│   └── ...
└── Debug/
    └── TestScene.unity  # デバッグ用
```

### シーンタイプ別ガイド

| シーンタイプ | loadMode | 用途 |
|------------|----------|------|
| Boot | single | 起動時初期化、GameManagerなど |
| Menu/Title | single | 画面単位の切り替え |
| Level | single | ゲームプレイ本体 |
| UI Overlay | additive | 複数レベルで共有するUI |
| Audio | additive | BGM/SE管理（DontDestroyOnLoad） |

---

## 🏗️ 3層アーキテクチャ

### ツール選択ガイド

| 目的 | 推奨レイヤー | 例 |
|------|------------|-----|
| UIシステム構築 | High-Level GameKit UI | UICommand, UIBinding, UIList |
| 演出・エフェクト | High-Level GameKit Presentation | Effect, Feedback, VFX, Audio |
| コード解析・整合性検証 | High-Level GameKit Logic | validate_integrity, class_catalog, dependency_graph |
| 複数オブジェクト一括処理 | Mid-Level Batch | Transform配置, Physics設定 |
| UI構築（UGUI） | Mid-Level UI | ui_foundation, ui_hierarchy, ui_state |
| UI構築（UI Toolkit） | Mid-Level UITK | uitk_document, uitk_asset |
| 詳細な個別制御 | Low-Level CRUD | GameObject/Component操作 |

### 🔄 PDCAワークフロー (開発サイクル)

すべての開発作業は **Plan → Do → Check → Act** のサイクルで進める。

#### P (Plan) - 計画・調査

変更前に現状を把握し、影響範囲を特定する。

```python
# 1. シーン全体の構造を確認
unity_scene_crud(operation='inspect', includeHierarchy=True)

# 2. 変更対象を事前調査（inspect操作）
unity_component_crud(operation='inspect', gameObjectPath='Player', includeProperties=True)

# 3. 影響範囲の事前調査（削除・移動・リネーム前に必須）
unity_scene_reference_graph(
    operation='findReferencesTo',
    objectPath='TargetObject'
)

# 4. クラス依存関係の事前把握（スクリプト変更前）
unity_class_dependency_graph(
    operation='analyzeClass',
    target='TargetClass'
)

# 5. 利用可能な型の調査
unity_class_catalog(operation='listTypes', typeKind='MonoBehaviour', searchPath='Assets/Scripts')
```

#### D (Do) - 実行

計画に基づき、適切なレイヤーのツールで変更を実行する。

```python
# High-Level GameKit: UIシステム構築
unity_gamekit_ui_command(
    operation='createCommandPanel',
    panelId='CommandPanel',
    canvasPath='Canvas',
    commands=[{'name': 'Attack', 'commandType': 'action', 'label': '攻撃'}],
    targetType='actor',
    targetActorId='player'
)

# Mid-Level Batch: バッチ操作・プリセット適用
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'], preset='character')
unity_transform_batch(operation='arrangeLine', gameObjectPaths=[...], startPosition=..., endPosition=...)

# Low-Level CRUD: 詳細な個別制御
unity_component_crud(operation='add', gameObjectPath='Player', componentType='...', propertyChanges={...})

# コード生成後はコンパイル待機
unity_compilation_await(operation='await', timeoutSeconds=60)
```

#### C (Check) - 確認・検証

変更後は必ず以下のツールで品質を検証する。**特に削除・移動・リネーム・参照変更後は必須。**

```python
# 1. シーン整合性チェック（Missing Script、null参照、壊れたイベント/Prefab検出）
unity_validate_integrity(operation='all')

# 2. 参照・イベント・階層の統合検証
unity_scene_relationship_graph(operation='analyzeAll')

# 3. クラス依存関係の健全性確認（スクリプト変更後）
unity_class_dependency_graph(
    operation='analyzeAssembly',
    target='Assembly-CSharp',
    includeUnityTypes=False
)

# 4. 特定オブジェクトの参照追跡
unity_scene_reference_graph(
    operation='analyzeObject',
    objectPath='ChangedObject'
)

# 5. コンソールログでエラー・警告を確認
unity_console_log(operation='getErrors')
```

#### A (Act) - 改善・対処

Checkで発見した問題を修正し、動作を確認する。

```python
# 1. 壊れた参照の修復（イベント再接続）
unity_event_wiring(
    operation='wire',
    source={'gameObject': 'Button', 'component': 'Button', 'event': 'onClick'},
    target={'gameObject': 'GameManager', 'method': 'StartGame'}
)

# 2. 不要な参照・コンポーネントの除去
unity_component_crud(operation='remove', gameObjectPath='Object', componentType='BrokenScript')

# 3. Missing Scriptの自動除去
unity_validate_integrity(operation='removeMissingScripts')

# 4. プレイモードで実際の動作を確認
unity_playmode_control(operation='play')

# 5. ランタイムエラーの確認
unity_console_log(operation='getErrors')

# 6. プレイモード停止
unity_playmode_control(operation='stop')

# 問題が残っている場合 → Plan に戻って再調査
```

#### PDCAチェックリスト

| フェーズ | 必須アクション | 使用ツール |
|---------|--------------|-----------|
| **Plan** | 現状把握、影響調査 | inspect操作, scene_reference_graph, class_dependency_graph, class_catalog |
| **Do** | 適切なレイヤーで実行 | GameKit, Batch, CRUD, compilation_await |
| **Check** | 整合性・依存関係検証 | validate_integrity, scene_relationship_graph, class_dependency_graph, console_log |
| **Act** | 問題修正・動作確認 | event_wiring, CRUD, validate_integrity, playmode_control, console_log |

---

## 🔍 High-Level GameKit - Logic Pillar (解析・整合性)

### unity_validate_integrity - シーン整合性検証

シーン内の壊れた参照、Missing Script、不正なイベント/Prefabを検出。

```python
# 全チェック実行
unity_validate_integrity(operation='all')

# 個別チェック
unity_validate_integrity(operation='missingScripts')
unity_validate_integrity(operation='nullReferences')
unity_validate_integrity(operation='brokenEvents')
unity_validate_integrity(operation='brokenPrefabs')

# Missing Script自動除去（Undo可能）
unity_validate_integrity(operation='removeMissingScripts')

# サブツリー限定
unity_validate_integrity(operation='all', rootPath='Canvas/Panel')
```

### unity_class_catalog - クラスカタログ

プロジェクト内の型（MonoBehaviour, ScriptableObject, enum等）を列挙・詳細表示。

```python
# MonoBehaviour一覧
unity_class_catalog(operation='listTypes', typeKind='MonoBehaviour', searchPath='Assets/Scripts')

# 特定の型を詳細表示
unity_class_catalog(operation='inspectType', className='PlayerController', includeFields=True, includeMethods=True)

# 名前パターンで検索
unity_class_catalog(operation='listTypes', namePattern='*Controller', maxResults=50)
```

### unity_scene_reference_graph - シーン参照グラフ

シーン内のGameObject間のコンポーネント参照を解析。

```python
# シーン全体の参照グラフを取得
unity_scene_reference_graph(operation='analyzeScene')

# 特定オブジェクトからの参照を追跡
unity_scene_reference_graph(operation='analyzeObject', objectPath='Player')

# このオブジェクトを参照しているものを検索
unity_scene_reference_graph(operation='findReferencesTo', objectPath='Player')

# このオブジェクトが参照しているものを検索
unity_scene_reference_graph(operation='findReferencesFrom', objectPath='Player')

# 参照されていないオブジェクトを検出
unity_scene_reference_graph(operation='findOrphans')

# 出力形式: json, dot, mermaid, summary
unity_scene_reference_graph(operation='analyzeScene', format='mermaid')
```

### unity_class_dependency_graph - クラス依存関係グラフ

C#スクリプト間の依存関係を解析。

```python
# 特定クラスの依存関係
unity_class_dependency_graph(operation='analyzeClass', target='PlayerController')

# アセンブリ全体の解析
unity_class_dependency_graph(operation='analyzeAssembly', target='Assembly-CSharp')

# 名前空間単位の解析
unity_class_dependency_graph(operation='analyzeNamespace', target='MyGame.Combat')

# 依存先・被依存の検索
unity_class_dependency_graph(operation='findDependents', target='HealthSystem')
unity_class_dependency_graph(operation='findDependencies', target='PlayerController')

# 出力形式: json, dot, mermaid, summary
unity_class_dependency_graph(operation='analyzeClass', target='PlayerController', format='mermaid')
```

### unity_scene_relationship_graph - シーン遷移グラフ

シーン間の遷移関係（SceneManager.LoadScene呼び出し、SceneFlow等）を解析。

```python
# プロジェクト全体のシーン遷移
unity_scene_relationship_graph(operation='analyzeAll')

# 特定シーンの遷移先
unity_scene_relationship_graph(operation='analyzeScene', scenePath='Assets/Scenes/Title.unity')

# 特定シーンへの遷移元
unity_scene_relationship_graph(operation='findTransitionsTo', scenePath='Assets/Scenes/Level1.unity')

# Build Settings検証
unity_scene_relationship_graph(operation='validateBuildSettings')
```

---

## 🎮 High-Level GameKit - UI Pillar (UIシステム)

GameKit UIツールはUI Toolkit（UXML/USS）ベースのコード生成でUIシステムを構築する。生成されたスクリプトはUnity-AI-Forgeに依存しないスタンドアロンコード。

### GameKit UI Command - UIコマンドパネル

```python
unity_gamekit_ui_command(
    operation='createCommandPanel',
    panelId='CommandPanel',
    canvasPath='Canvas',
    commands=[
        {'name': 'Move', 'commandType': 'move', 'label': '移動'},
        {'name': 'Attack', 'commandType': 'action', 'label': '攻撃'},
    ],
    layout='horizontal',  # 'vertical'|'grid'
    targetType='actor',   # 'manager'
    targetActorId='player_001'
)

# コマンド追加
unity_gamekit_ui_command(operation='addCommand', panelId='CommandPanel', command={'name': 'Heal', 'commandType': 'action', 'label': '回復'})
```

### GameKit UI Binding - 宣言的UIデータバインディング

```python
unity_gamekit_ui_binding(
    operation='create',
    targetPath='Canvas/HUD/HPBar',
    bindingId='player_hp_bar',
    sourceType='health',  # 'economy'|'timer'|'custom'
    sourceId='player_health',
    format='percent'  # 'raw'|'ratio'|'formatted'
)

# 値範囲設定
unity_gamekit_ui_binding(operation='setRange', bindingId='player_hp_bar', min=0, max=100)
```

### GameKit UI List - 動的リスト/グリッド

```python
unity_gamekit_ui_list(
    operation='create',
    targetPath='Canvas/InventoryPanel',
    listId='inventory_list',
    layout='grid',  # 'vertical'|'horizontal'
    gridColumns=4
)

# アイテム操作
unity_gamekit_ui_list(operation='addItem', listId='inventory_list', itemData={'id': 'sword', 'name': '剣'})
unity_gamekit_ui_list(operation='selectItem', listId='inventory_list', index=0)
unity_gamekit_ui_list(operation='removeItem', listId='inventory_list', index=0)
```

### GameKit UI Slot - アイテムスロット

```python
# 単体スロット
unity_gamekit_ui_slot(
    operation='create',
    targetPath='Canvas/Equipment/WeaponSlot',
    slotId='weapon_slot',
    slotType='equipment',  # 'storage'|'quickslot'|'trash'
    acceptTags=['weapon']
)

# スロットバー（複数スロット一括作成）
unity_gamekit_ui_slot(
    operation='createSlotBar',
    barId='quickbar',
    targetPath='Canvas/QuickBar',
    slotCount=8,
    slotType='quickslot'
)
```

### GameKit UI Selection - 選択グループ

```python
unity_gamekit_ui_selection(
    operation='create',
    targetPath='Canvas/TabPanel',
    selectionId='tab_selection',
    selectionMode='radio',  # 'toggle'|'checkbox'|'tab'
    defaultSelected=0
)
```

---

## 🎨 High-Level GameKit - Presentation Pillar (演出)

演出・エフェクト系のコード生成でスタンドアロンなスクリプトを自動生成。

### GameKit Effect - 複合エフェクト

```python
unity_gamekit_effect(
    operation='create',
    targetPath='Effects/Explosion',
    effectId='explosion',
    components=[
        {'type': 'particle', 'prefabPath': 'Assets/Prefabs/Explosion.prefab'},
        {'type': 'sound', 'clipPath': 'Assets/Audio/SFX/Explosion.wav'},
        {'type': 'cameraShake', 'intensity': 0.5, 'duration': 0.3},
        {'type': 'screenFlash', 'color': {'r': 1, 'g': 0.8, 'b': 0.3, 'a': 0.5}, 'duration': 0.1}
    ]
)

# マネージャー作成（エフェクトの一元管理）
unity_gamekit_effect(operation='createManager', targetPath='EffectManager')
unity_gamekit_effect(operation='registerEffect', effectId='explosion')
```

### GameKit Feedback - ゲームフィール演出

```python
unity_gamekit_feedback(
    operation='create',
    targetPath='FeedbackManager',
    feedbackId='hit_feedback',
    components=[
        {'type': 'hitstop', 'duration': 0.05},
        {'type': 'screenShake', 'intensity': 0.3, 'duration': 0.15},
        {'type': 'flash', 'color': {'r': 1, 'g': 1, 'b': 1, 'a': 0.5}, 'duration': 0.05}
    ]
)

# 強度設定
unity_gamekit_feedback(operation='setIntensity', feedbackId='hit_feedback', intensity=1.5)
```

### GameKit VFX - ビジュアルエフェクト

```python
unity_gamekit_vfx(
    operation='create',
    targetPath='Effects/Explosion',
    vfxId='explosion_vfx',
    particlePrefabPath='Assets/Prefabs/Explosion.prefab',
    usePooling=True,
    poolSize=10
)

# マルチプライヤー設定
unity_gamekit_vfx(operation='setMultipliers', vfxId='explosion_vfx', duration=1.5, size=2.0, emission=3.0)
```

### GameKit Audio - オーディオ再生

```python
unity_gamekit_audio(
    operation='create',
    targetPath='AudioManager/BGM',
    audioId='bgm_main',
    audioType='music',  # 'sfx'|'ambient'|'voice'|'ui'
    audioClipPath='Assets/Audio/BGM/Main.mp3',
    loop=True,
    fadeInDuration=2.0
)

# 設定変更
unity_gamekit_audio(operation='setVolume', audioId='bgm_main', volume=0.8)
unity_gamekit_audio(operation='setPitch', audioId='bgm_main', pitch=1.2)
```

### GameKit Animation Sync - アニメーション同期

```python
unity_gamekit_animation_sync(
    operation='create',
    targetPath='Player',
    syncId='player_anim_sync',
    syncSource='rigidbody2d',  # 'rigidbody3d'|'transform'|'health'|'custom'
    animatorPath='Player'
)

# 同期ルール追加
unity_gamekit_animation_sync(
    operation='addSyncRule',
    syncId='player_anim_sync',
    parameterName='Speed',
    sourceField='velocity.magnitude'
)

# トリガールール追加
unity_gamekit_animation_sync(
    operation='addTriggerRule',
    syncId='player_anim_sync',
    triggerName='Hit',
    eventSource='health',
    eventType='damage'
)
```

---

## ⚡ Mid-Level Batch Tools

### Transform Batch - 配置とリネーム

```python
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=['Obj1', 'Obj2'], radius=5.0)
unity_transform_batch(operation='arrangeLine', gameObjectPaths=[...], startPosition={'x': 0, 'y': 0, 'z': 0}, endPosition={'x': 10, 'y': 0, 'z': 0})
unity_transform_batch(operation='renameSequential', gameObjectPaths=[...], baseName='Enemy', startIndex=1, padding=3)
unity_transform_batch(operation='createMenuList', parentPath='Canvas/Menu', prefabPath='Assets/Prefabs/UI/MenuItem.prefab', names=['Start', 'Options', 'Quit'])
```

### RectTransform Batch - UIレイアウト

```python
unity_rectTransform_batch(operation='setAnchors', gameObjectPaths=[...], anchorPreset='topLeft')
unity_rectTransform_batch(operation='alignToParent', gameObjectPaths=[...], preset='topLeft')
unity_rectTransform_batch(operation='distributeHorizontal', gameObjectPaths=[...], spacing=10)
unity_rectTransform_batch(operation='matchSize', gameObjectPaths=[...], sourceObjectPath='Reference', matchMode='both')
```

### Physics Bundle - 物理プリセット

```python
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'], preset='character')
# 2Dプリセット: 'dynamic'|'kinematic'|'static'|'character'|'platformer'|'topDown'|'vehicle'|'projectile'
```

### Camera Rig - カメラ設定

```python
unity_camera_rig(operation='createRig', rigType='follow', rigName='MainCamera', targetPath='Player', offset={'x': 0, 'y': 5, 'z': -10})
# rigType: 'follow'|'orbit'|'splitScreen'|'fixed'|'dolly'
```

### UI Foundation - UI基礎要素 (UGUI)

```python
unity_ui_foundation(operation='createCanvas', name='GameUI', renderMode='screenSpaceOverlay')
unity_ui_foundation(operation='createPanel', name='Panel', parentPath='GameUI', anchorPreset='middleCenter')
unity_ui_foundation(operation='createButton', name='Button', parentPath='GameUI', text='Click')
unity_ui_foundation(operation='createText', name='Label', parentPath='GameUI', text='Score: 0')
unity_ui_foundation(operation='addLayoutGroup', targetPath='GameUI/Panel', layoutType='Vertical', spacing=10)
```

### UI Hierarchy - 宣言的UI構築

```python
unity_ui_hierarchy(
    operation='create',
    parentPath='Canvas',
    hierarchy={
        'type': 'panel',
        'name': 'Menu',
        'children': [
            {'type': 'text', 'name': 'Title', 'text': 'Game Menu', 'fontSize': 32},
            {'type': 'button', 'name': 'StartBtn', 'text': 'Start Game'},
        ],
        'layout': 'Vertical',
        'spacing': 20
    }
)

# 表示切替（CanvasGroup利用）
unity_ui_hierarchy(operation='show', targetPath='Canvas/Menu')
unity_ui_hierarchy(operation='hide', targetPath='Canvas/Menu')
```

### UI State - UI状態管理

```python
# 状態定義
unity_ui_state(operation='defineState', rootPath='Canvas/Menu', stateName='mainMenu', elements=[
    {'path': 'Canvas/Menu/MainPanel', 'active': True, 'visible': True},
    {'path': 'Canvas/Menu/SettingsPanel', 'active': False}
])

# 状態適用
unity_ui_state(operation='applyState', rootPath='Canvas/Menu', stateName='mainMenu')

# 状態グループ（排他的）
unity_ui_state(operation='createStateGroup', rootPath='Canvas/Menu', groupName='menuScreens', states=['mainMenu', 'settings', 'credits'])
```

### UI Navigation - キーボード/ゲームパッドナビゲーション

```python
# 自動セットアップ
unity_ui_navigation(operation='autoSetup', rootPath='Canvas/Menu', direction='vertical')

# 明示的ナビゲーション設定
unity_ui_navigation(operation='setExplicit', gameObjectPath='Canvas/Menu/StartBtn',
    up='Canvas/Menu/QuitBtn', down='Canvas/Menu/OptionsBtn')

# 最初の選択要素設定
unity_ui_navigation(operation='setFirstSelected', gameObjectPath='Canvas/Menu/StartBtn')
```

### UI Toolkit - UXML/USS/PanelSettings

```python
# UXMLファイル作成
unity_uitk_asset(
    operation='createUXML',
    assetPath='Assets/UI/MainMenu.uxml',
    elements=[
        {'type': 'VisualElement', 'name': 'root', 'classes': ['container'], 'children': [
            {'type': 'Label', 'name': 'title', 'text': 'Main Menu'},
            {'type': 'Button', 'name': 'startBtn', 'text': 'Start Game'}
        ]}
    ]
)

# USSスタイルシート作成
unity_uitk_asset(
    operation='createUSS',
    assetPath='Assets/UI/MainMenu.uss',
    rules=[
        {'selector': '.container', 'properties': {'flex-direction': 'column', 'align-items': 'center'}},
        {'selector': '#title', 'properties': {'font-size': '48px', 'color': 'white'}}
    ]
)

# テンプレートから作成
unity_uitk_asset(operation='createFromTemplate', template='menu', assetPath='Assets/UI/Menu')

# UIDocumentをシーンに配置
unity_uitk_document(operation='create', gameObjectPath='UI/MainMenu', uxmlPath='Assets/UI/MainMenu.uxml')
```

### Material Bundle - マテリアル設定

```python
unity_material_bundle(operation='create', materialPath='Assets/Materials/Player.mat', shader='Standard')
unity_material_bundle(operation='setColor', materialPath='Assets/Materials/Player.mat', propertyName='_Color', color={'r': 1, 'g': 0, 'b': 0, 'a': 1})
unity_material_bundle(operation='applyPreset', materialPath='Assets/Materials/Glass.mat', preset='glass')
# プリセット: 'unlit'|'lit'|'transparent'|'cutout'|'fade'|'sprite'|'ui'|'emissive'|'metallic'|'glass'
```

### Light Bundle - ライト設定

```python
unity_light_bundle(operation='create', gameObjectPath='Lights/MainLight', lightType='directional', color={'r': 1, 'g': 0.95, 'b': 0.8}, intensity=1.0)
unity_light_bundle(operation='applyPreset', gameObjectPath='Lights/MainLight', preset='sunset')
# ライトプリセット: 'daylight'|'moonlight'|'warm'|'cool'|'spotlight'|'candle'|'neon'
# セットアッププリセット: 'daylight'|'nighttime'|'indoor'|'dramatic'|'studio'|'sunset'
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')
```

### Particle Bundle - パーティクル設定

```python
unity_particle_bundle(operation='create', gameObjectPath='Effects/Fire', preset='fire')
unity_particle_bundle(operation='update', gameObjectPath='Effects/Fire', startSize=2.0, startLifetime=3.0)
# プリセット: 'explosion'|'fire'|'smoke'|'sparkle'|'rain'|'snow'|'dust'|'trail'|'hit'|'heal'|'magic'|'leaves'
```

### Audio Source Bundle - オーディオソース

```python
unity_audio_source_bundle(operation='create', gameObjectPath='Audio/BGM', preset='music', clipPath='Assets/Audio/BGM.mp3')
# プリセット: 'music'|'sfx'|'ambient'|'voice'|'ui'
```

### Event Wiring - UnityEventの接続

```python
# イベント接続
unity_event_wiring(
    operation='wire',
    source={'gameObject': 'Canvas/StartButton', 'component': 'Button', 'event': 'onClick'},
    target={'gameObject': 'GameManager', 'method': 'StartGame'}
)

# 引数付きイベント
unity_event_wiring(
    operation='wire',
    source={'gameObject': 'Canvas/Slider', 'component': 'Slider', 'event': 'onValueChanged'},
    target={'gameObject': 'AudioManager', 'method': 'SetVolume', 'mode': 'Float'}
)

# イベント解除
unity_event_wiring(operation='unwire', source={'gameObject': 'Canvas/StartButton', 'component': 'Button', 'event': 'onClick'})

# 一括接続
unity_event_wiring(operation='wireMultiple', wirings=[
    {'source': {'gameObject': 'Btn1', 'component': 'Button', 'event': 'onClick'}, 'target': {'gameObject': 'Mgr', 'method': 'Action1'}},
    {'source': {'gameObject': 'Btn2', 'component': 'Button', 'event': 'onClick'}, 'target': {'gameObject': 'Mgr', 'method': 'Action2'}}
])

# イベント一覧確認
unity_event_wiring(operation='listEvents', gameObjectPath='Canvas/StartButton')
```

---

## 🔧 Low-Level CRUD Tools

### Scene & GameObject

```python
unity_scene_crud(operation='inspect', includeHierarchy=True)
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Level1.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Level1.unity', loadMode='single')

unity_gameobject_crud(operation='create', name='Player', parentPath='Characters')
unity_gameobject_crud(operation='create', name='Enemy', parentPath='Enemies',
    components=[{'type': 'UnityEngine.Rigidbody2D', 'properties': {'gravityScale': 0}}])
unity_gameobject_crud(operation='update', gameObjectPath='Player', tag='Player', layer='Player', active=True)
unity_gameobject_crud(operation='findMultiple', pattern='Enemy*', maxResults=100)
```

### Component

```python
unity_component_crud(operation='add', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'gravityScale': 0})
unity_component_crud(operation='update', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'mass': 2.0})
unity_component_crud(operation='inspect', gameObjectPath='Player', componentType='*', includeProperties=True)
unity_component_crud(operation='addMultiple', pattern='Enemy*', componentType='UnityEngine.BoxCollider2D')
```

#### Unity Object参照 (propertyChanges内)

```python
{'$ref': 'Assets/Materials/Player.mat'}  # アセット参照
{'$ref': 'Canvas/Panel/Button'}          # シーンオブジェクト参照
```

### Asset & Script

```python
unity_asset_crud(operation='create', assetPath='Assets/Data/config.json', content='{"version": 1}')
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Player.cs', content='using UnityEngine;\n...')
unity_asset_crud(operation='updateImporter', assetPath='Assets/Textures/sprite.png', propertyChanges={'textureType': 'Sprite'})
```

### ScriptableObject

```python
unity_scriptableObject_crud(operation='create', typeName='MyGame.GameConfig', assetPath='Assets/Data/GameConfig.asset', properties={'version': 1})
unity_scriptableObject_crud(operation='findByType', typeName='MyGame.GameConfig', searchPath='Assets/Data')
```

### Prefab

```python
unity_prefab_crud(operation='create', gameObjectPath='Player', prefabPath='Assets/Prefabs/Player.prefab')
unity_prefab_crud(operation='instantiate', prefabPath='Assets/Prefabs/Enemy.prefab', parentPath='Enemies', position={'x': 0, 'y': 0, 'z': 5})
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Player')
```

### Vector Sprite Convert

```python
unity_vector_sprite_convert(operation='createPrimitive', primitiveType='circle', width=64, height=64, color={'r': 1, 'g': 0, 'b': 0, 'a': 1})
```

### Project Settings

```python
unity_projectSettings_crud(operation='read', category='physics2d', property='gravity')
unity_projectSettings_crud(operation='write', category='tagsLayers', property='addTag', value='Enemy')
unity_projectSettings_crud(operation='addSceneToBuild', scenePath='Assets/Scenes/Level1.unity')
unity_projectSettings_crud(operation='listBuildScenes')
```

---

## 🔌 Utility Tools

### unity_ping - 接続確認

```python
unity_ping()  # ブリッジ接続状態を確認
```

### unity_compilation_await - コンパイル待機

```python
unity_compilation_await(operation='await', timeoutSeconds=60)
unity_compilation_await(operation='status')  # 現在のコンパイル状態確認
```

### unity_playmode_control - プレイモード制御

```python
unity_playmode_control(operation='play')     # プレイモード開始
unity_playmode_control(operation='stop')     # プレイモード停止
unity_playmode_control(operation='pause')    # 一時停止
unity_playmode_control(operation='unpause')  # 再開
unity_playmode_control(operation='step')     # 1フレーム進める
unity_playmode_control(operation='getState') # 現在の状態確認
```

### unity_console_log - コンソールログ取得

```python
unity_console_log(operation='getRecent', count=50)       # 最新ログ取得
unity_console_log(operation='getErrors')                  # エラーのみ
unity_console_log(operation='getWarnings')                # 警告のみ
unity_console_log(operation='getCompilationErrors')       # コンパイルエラー詳細
unity_console_log(operation='getSummary')                 # ログ件数サマリー
unity_console_log(operation='clear')                      # コンソールクリア
```

---

## 🔄 Batch Sequential Execute

複数のUnity操作を順番に実行し、エラー時に停止して再開可能。

```python
unity_batch_sequential_execute(
    operations=[
        {'tool': 'unity_gameobject_crud', 'arguments': {'operation': 'create', 'name': 'Enemy1', 'parentPath': 'Enemies'}},
        {'tool': 'unity_component_crud', 'arguments': {'operation': 'add', 'gameObjectPath': 'Enemies/Enemy1', 'componentType': 'UnityEngine.Rigidbody2D'}},
    ],
    resume=False,
    stop_on_error=True
)

# エラーから再開
unity_batch_sequential_execute(resume=True)
```

---

## ⚡ Performance Best Practices

### 高速化テクニック

1. **`includeProperties=False`**: コンポーネント存在確認のみ（10倍高速）
2. **`propertyFilter`**: 必要なプロパティのみ取得
3. **`maxResults`**: 大量操作時のタイムアウト防止（デフォルト1000）
4. **`stopOnError=False`**: バッチ処理でエラー時も続行

### バッチ操作（推奨）

```python
# ❌ 避ける: ループ内で個別ツール呼び出し

# ✅ 推奨1: Mid-Level Batchツール
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=[...])

# ✅ 推奨2: *Multiple 操作
unity_component_crud(operation='addMultiple', pattern='Enemy*', componentType='...')

# ✅ 推奨3: 複雑な多段階処理
unity_batch_sequential_execute(operations=[...])
```

---

## 🔧 Troubleshooting

### 接続エラー

1. `unity_ping` で接続確認
2. Unity Editor: Tools > MCP Assistant が起動しているか確認
3. ポート7077が使用可能か確認

### コンパイルエラー

- C#スクリプト作成/更新後は `unity_compilation_await(operation='await')` で待機
- `unity_console_log(operation='getCompilationErrors')` でエラー詳細を確認

### タイムアウト

1. `maxResults` を減らす（デフォルト1000 → 100以下）
2. `includeProperties=False` で高速化
3. Mid-Level Batchツールを使用

### 整合性エラー

1. `unity_validate_integrity(operation='all')` で全チェック
2. `unity_validate_integrity(operation='removeMissingScripts')` でMissing Script自動除去
3. `unity_scene_reference_graph(operation='findOrphans')` で孤立オブジェクト検出

---

## 📚 Unity標準コンポーネント リファレンス

`unity_component_crud` の `componentType` に指定する完全型名と主要プロパティ一覧。
数値で示すenum値は `propertyChanges` でint指定可能。

### Transform

| componentType | 主要プロパティ |
|--------------|--------------|
| `Transform` | position, rotation, localScale, localPosition, localRotation |
| `RectTransform` | anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot, offsetMin, offsetMax |

### Physics 2D

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Rigidbody2D` | bodyType (0=Dynamic,1=Kinematic,2=Static), mass, linearDamping, angularDamping, gravityScale, constraints, collisionDetectionMode |
| `UnityEngine.BoxCollider2D` | size, offset, isTrigger, usedByComposite |
| `UnityEngine.CircleCollider2D` | radius, offset, isTrigger |
| `UnityEngine.CapsuleCollider2D` | size, offset, direction (0=Vertical,1=Horizontal), isTrigger |
| `UnityEngine.PolygonCollider2D` | points, offset, isTrigger |
| `UnityEngine.EdgeCollider2D` | points, offset, edgeRadius, isTrigger |
| `UnityEngine.CompositeCollider2D` | geometryType (0=Outlines,1=Polygons), generationType |

### Physics 3D

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Rigidbody` | mass, drag, angularDrag, useGravity, isKinematic, constraints, collisionDetectionMode |
| `UnityEngine.BoxCollider` | center, size, isTrigger |
| `UnityEngine.SphereCollider` | center, radius, isTrigger |
| `UnityEngine.CapsuleCollider` | center, radius, height, direction (0=X,1=Y,2=Z), isTrigger |
| `UnityEngine.MeshCollider` | convex, isTrigger, sharedMesh |
| `UnityEngine.CharacterController` | center, radius, height, slopeLimit, stepOffset, skinWidth |

### Rendering 2D

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.SpriteRenderer` | sprite, color, flipX, flipY, sortingLayerName, sortingOrder, drawMode, maskInteraction |
| `UnityEngine.SpriteMask` | sprite, alphaCutoff, isCustomRangeActive |

### Rendering 3D

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.MeshFilter` | sharedMesh |
| `UnityEngine.MeshRenderer` | sharedMaterials, shadowCastingMode, receiveShadows, sortingLayerName, sortingOrder |
| `UnityEngine.SkinnedMeshRenderer` | sharedMesh, sharedMaterials, rootBone, quality |
| `UnityEngine.LineRenderer` | startWidth, endWidth, startColor, endColor, positionCount, useWorldSpace, loop |
| `UnityEngine.TrailRenderer` | time, startWidth, endWidth, startColor, endColor, minVertexDistance |

### Camera & Light

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Camera` | fieldOfView, orthographic, orthographicSize, nearClipPlane, farClipPlane, clearFlags (1=Skybox,2=SolidColor,3=Depth,4=Nothing), backgroundColor, cullingMask, depth, targetTexture |
| `UnityEngine.Light` | type (0=Spot,1=Directional,2=Point,3=Area), color, intensity, range, spotAngle, shadows (0=None,1=Hard,2=Soft) |

### Audio

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.AudioSource` | clip, volume, pitch, loop, playOnAwake, spatialBlend (0=2D,1=3D), minDistance, maxDistance, outputAudioMixerGroup |
| `UnityEngine.AudioListener` | _(プロパティ変更不要、シーンに1つ)_ |

### Animation

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Animator` | runtimeAnimatorController, avatar, applyRootMotion, updateMode (0=Normal,1=AnimatePhysics,2=UnscaledTime), cullingMode |

### UI - Canvas構造

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Canvas` | renderMode (0=ScreenSpaceOverlay,1=ScreenSpaceCamera,2=WorldSpace), sortingOrder, worldCamera, planeDistance |
| `UnityEngine.CanvasScaler` | uiScaleMode (0=ConstantPixelSize,1=ScaleWithScreenSize,2=ConstantPhysicalSize), referenceResolution, screenMatchMode, matchWidthOrHeight |
| `UnityEngine.UI.GraphicRaycaster` | ignoreReversedGraphics, blockingObjects |

### UI - 表示要素

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.UI.Image` | sprite, color, type (0=Simple,1=Sliced,2=Tiled,3=Filled), fillAmount, preserveAspect, raycastTarget |
| `UnityEngine.UI.RawImage` | texture, color, uvRect, raycastTarget |
| `UnityEngine.UI.Text` | text, font, fontSize, fontStyle, alignment, color, raycastTarget _(レガシー、TMPro推奨)_ |
| `TMPro.TextMeshProUGUI` | text, fontSize, fontStyle, alignment, color, enableAutoSizing, fontSizeMin, fontSizeMax, raycastTarget |

### UI - 入力要素

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.UI.Button` | interactable, transition (0=None,1=ColorTint,2=SpriteSwap,3=Animation), colors, navigation |
| `UnityEngine.UI.Toggle` | isOn, interactable, toggleTransition, group |
| `UnityEngine.UI.Slider` | value, minValue, maxValue, wholeNumbers, direction (0=LeftToRight,1=RightToLeft,2=BottomToTop,3=TopToBottom), interactable |
| `UnityEngine.UI.Dropdown` | value, options, interactable _(レガシー)_ |
| `TMPro.TMP_Dropdown` | value, options, interactable |
| `UnityEngine.UI.InputField` | text, characterLimit, contentType, lineType, interactable _(レガシー)_ |
| `TMPro.TMP_InputField` | text, characterLimit, contentType, lineType, interactable |
| `UnityEngine.UI.ScrollRect` | content, horizontal, vertical, movementType, elasticity, inertia, scrollSensitivity |
| `UnityEngine.UI.Scrollbar` | value, size, numberOfSteps, direction |

### UI - レイアウト

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.UI.HorizontalLayoutGroup` | spacing, padding, childAlignment, childForceExpandWidth, childForceExpandHeight, childControlWidth, childControlHeight |
| `UnityEngine.UI.VerticalLayoutGroup` | spacing, padding, childAlignment, childForceExpandWidth, childForceExpandHeight, childControlWidth, childControlHeight |
| `UnityEngine.UI.GridLayoutGroup` | cellSize, spacing, startCorner, startAxis, constraint, constraintCount, padding, childAlignment |
| `UnityEngine.UI.ContentSizeFitter` | horizontalFit (0=Unconstrained,1=MinSize,2=PreferredSize), verticalFit |
| `UnityEngine.UI.LayoutElement` | minWidth, minHeight, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight, ignoreLayout |

### UI - マスク

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.UI.Mask` | showMaskGraphic |
| `UnityEngine.UI.RectMask2D` | padding, softness |

### パーティクル

| componentType | 備考 |
|--------------|------|
| `UnityEngine.ParticleSystem` | サブモジュール構造のため `unity_particle_bundle` 推奨 |

### ナビゲーション (AI Pathfinding)

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.AI.NavMeshAgent` | speed, angularSpeed, acceleration, stoppingDistance, radius, height, avoidancePriority, areaMask |
| `UnityEngine.AI.NavMeshObstacle` | shape (0=Capsule,1=Box), center, size, radius, height, carve, carvingMoveThreshold |

### イベントシステム

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.EventSystems.EventSystem` | firstSelectedGameObject, sendNavigationEvents |
| `UnityEngine.EventSystems.StandaloneInputModule` | horizontalAxis, verticalAxis, submitButton, cancelButton |

### ビデオ

| componentType | 主要プロパティ |
|--------------|--------------|
| `UnityEngine.Video.VideoPlayer` | source, url, clip, playOnAwake, isLooping, renderMode, targetCamera, audioOutputMode |

---

Unity-AI-Forge v{VERSION} - 49 Tools, 3-Layer Architecture, 3-Pillar GameKit
