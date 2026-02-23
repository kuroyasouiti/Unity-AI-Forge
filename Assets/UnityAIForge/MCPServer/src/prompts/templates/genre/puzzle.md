# パズルゲーム 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

落ち物・スライド・ソコバン・マッチ系など多様なサブジャンルを含むが、
共通するのは「グリッドベースの状態管理」「Undo/Redo」「クリア条件判定」「UI 中心設計」である。
物理演算は最小限に留め、ゲームロジックをカスタムスクリプトと ScriptableObject で完全にデータ化することが
ゲームバランス調整の効率化に直結する。
GameKit の UI Pillar を中心に構築し、Presentation Pillar でクリア演出を表現する。

---

## シーン構成

```
Scenes/
  Boot.unity          # 初期化・マスターデータ
  MainMenu.unity      # タイトル・ステージ選択
  StageSelect.unity   # ステージマップ UI
  Puzzle.unity        # メインゲームシーン（全ステージ共通）
  StageClear.unity    # クリア演出
  Tutorial.unity      # チュートリアル
```

Puzzle.unity の GameObject 構成例:

```
[Puzzle Scene]
  - PuzzleManager         # ゲームフロー管理（カスタムスクリプト）
  - GridManager           # グリッド状態・セル管理（カスタムスクリプト）
  - Grid/
  |   - Cell_0_0 ... Cell_N_M   # グリッドセル GameObject
  - UI/
  |   - Canvas_HUD
  |       - MoveCounter        # 手数表示
  |       - TimerDisplay       # タイム表示
  |       - ScoreDisplay       # スコア表示
  |       - CommandButtons     # リセット・Undo
  - Audio/
  - FeedbackManager
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Puzzle/
      PuzzleManager.cs       # 手動作成: ゲームフロー状態管理
      GridManager.cs         # 手動作成: グリッド状態管理
      UndoManager.cs         # 手動作成: Command パターン
      ClearCondition.cs      # 手動作成: クリア条件判定
    UI/
      HUDCommand.cs          # 生成: unity_gamekit_ui_command
      ScoreBinding.cs        # 生成: unity_gamekit_ui_binding
      MoveBinding.cs         # 生成: unity_gamekit_ui_binding
      StageSelectList.cs     # 生成: unity_gamekit_ui_list
      DifficultyTabs.cs      # 生成: unity_gamekit_ui_selection
    Presentation/
      ClearFeedback.cs       # 生成: unity_gamekit_feedback
      ClearVFX.cs            # 生成: unity_gamekit_vfx
      PuzzleAudio.cs         # 生成: unity_gamekit_audio
  Data/
    Stages/
      Stage_001.asset        # ScriptableObject: ステージデータ
      Stage_002.asset
    Difficulties/
      Easy.asset, Normal.asset, Hard.asset
  Prefabs/
    Cells/, Blocks/, UI/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: ステージデータ（ScriptableObject）

```python
# ステージデータ ScriptableObject（グリッドレイアウト・クリア条件）
unity_scriptableObject_crud(operation='create',
    typeName='StageData',
    assetPath='Assets/Data/Stages/Stage_001.asset',
    fields={
        'stageId': '001',
        'stageName': 'ステージ 1',
        'gridWidth': 8,
        'gridHeight': 8,
        'moveLimit': 30,
        'timeLimit': 120.0,
        'targetScore': 1000,
        'difficulty': 'Easy',
    })

# 難易度データ
unity_scriptableObject_crud(operation='create',
    typeName='DifficultyData',
    assetPath='Assets/Data/Difficulties/Easy.asset',
    fields={
        'difficultyName': 'かんたん',
        'moveLimitMultiplier': 1.5,
        'timeLimitMultiplier': 1.5,
        'scoreMultiplier': 0.5,
    })
```

### Step 2: シーン・グリッド構築

```python
# パズルシーン作成
unity_scene_crud(operation='create', sceneName='Puzzle',
    scenePath='Assets/Scenes/Puzzle.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Puzzle.unity')

# PuzzleManager（ゲームフロー制御）
unity_gameobject_crud(operation='create', name='PuzzleManager')
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Puzzle/PuzzleManager.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')

# GridManager（グリッド状態管理）
unity_gameobject_crud(operation='create', name='GridManager')

# グリッド親オブジェクト
unity_gameobject_crud(operation='create', name='Grid',
    position={'x': 0, 'y': 0, 'z': 0})

# セル GameObject を一括生成（8x8 グリッド例）
for row in range(8):
    for col in range(8):
        unity_gameobject_crud(operation='create',
            name=f'Cell_{col}_{row}',
            parentPath='Grid',
            position={'x': col * 1.0, 'y': row * 1.0, 'z': 0})

# SpriteRenderer を各セルに一括追加
unity_component_crud(operation='addMultiple',
    targets=[
        {'gameObjectPath': f'Grid/Cell_{col}_{row}',
         'componentType': 'SpriteRenderer'}
        for row in range(8) for col in range(8)
    ])
```

### Step 3: セルのコライダー設定

```python
# 各セルにクリック判定用 BoxCollider2D を追加
unity_component_crud(operation='addMultiple',
    targets=[
        {'gameObjectPath': f'Grid/Cell_{col}_{row}',
         'componentType': 'BoxCollider2D',
         'properties': {'size': {'x': 0.9, 'y': 0.9}}}
        for row in range(8) for col in range(8)
    ])
```

### Step 4: Undo/Redo コマンドパネル

```python
# HUD Canvas
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_HUD',
    renderMode='ScreenSpaceOverlay')

# Undo/Redo ボタン コマンドパネル
unity_gamekit_ui_command(operation='createCommandPanel',
    panelId='undo_cmd',
    canvasPath='Canvas_HUD',
    commands=[
        {'name': 'Undo',  'commandType': 'action', 'label': '戻す'},
        {'name': 'Redo',  'commandType': 'action', 'label': 'やり直す'},
        {'name': 'Reset', 'commandType': 'action', 'label': 'リセット'},
        {'name': 'Hint',  'commandType': 'action', 'label': 'ヒント'},
    ])
unity_compilation_await(operation='await')
```

### Step 5: HUD・スコア表示

```python
# 手数カウンター
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='MoveCounter', text='手数: 0/30',
    position={'x': -300, 'y': 250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/MoveCounter',
    bindingId='move_count', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# タイマー表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='TimerDisplay', text='2:00',
    position={'x': 0, 'y': 250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/TimerDisplay',
    bindingId='timer_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# スコア表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='ScoreDisplay', text='Score: 0',
    position={'x': 300, 'y': 250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/ScoreDisplay',
    bindingId='score_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')
```

### Step 6: 難易度・ステージ選択 UI

```python
# 難易度タブ（Easy / Normal / Hard）
unity_gamekit_ui_selection(operation='create',
    targetPath='Canvas_StageSelect/DifficultyTabs',
    selectionId='difficulty_sel',
    selectionMode='radio')
unity_compilation_await(operation='await')

unity_gamekit_ui_selection(operation='setItems',
    selectionId='difficulty_sel',
    items=[
        {'id': 'easy',   'label': 'かんたん'},
        {'id': 'normal', 'label': 'ふつう'},
        {'id': 'hard',   'label': 'むずかしい'},
    ])

# ステージリスト
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_StageSelect/StageList',
    listId='stage_list',
    layout='grid', gridColumns=5)
unity_compilation_await(operation='await')
```

### Step 7: クリア演出・効果音

```python
# フィードバックマネージャ
unity_gameobject_crud(operation='create', name='FeedbackManager')

# クリア時フィードバック（画面フラッシュ）
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='stage_clear',
    components=[
        {'type': 'screenFlash', 'color': {'r':1,'g':1,'b':0.5,'a':0.6}, 'duration': 0.3},
    ])
unity_compilation_await(operation='await')

# クリア VFX
unity_gamekit_vfx(operation='create', targetPath='FX/ClearEffect',
    vfxId='clear_vfx')
unity_compilation_await(operation='await')

# BGM
unity_gamekit_audio(operation='create', targetPath='Audio/BGM',
    audioId='puzzle_bgm', audioClipPath='Assets/Audio/BGM/Puzzle.mp3',
    loop=True)
unity_compilation_await(operation='await')

# 操作音
unity_gamekit_audio(operation='create', targetPath='Audio/SFX',
    audioId='sfx_move', audioClipPath='Assets/Audio/SFX/BlockMove.wav')
unity_compilation_await(operation='await')

unity_gamekit_audio(operation='create', targetPath='Audio/SFX',
    audioId='sfx_clear', audioClipPath='Assets/Audio/SFX/Clear.wav')
unity_compilation_await(operation='await')
```

### Step 8: イベント接続・検証

```python
# ボタンクリックイベントを接続
unity_event_wiring(operation='wire',
    sourcePath='Canvas_HUD/CommandPanel/Undo',
    eventName='onClick',
    targetPath='PuzzleManager',
    methodName='OnUndoClicked')

unity_event_wiring(operation='wire',
    sourcePath='Canvas_HUD/CommandPanel/Reset',
    eventName='onClick',
    targetPath='PuzzleManager',
    methodName='OnResetClicked')

# 整合性検証
unity_validate_integrity(operation='all')
```

---

## よくあるパターン

### Command パターンによる Undo/Redo

各操作（セル移動・ブロック配置）を Command オブジェクトとしてスタックに積む。
UndoManager で状態を管理し、`ui_command` の Undo ボタンで
スタックを pop して逆操作を実行する。
ロジックは `unity_asset_crud` でカスタムスクリプトとして作成する。

### コンボシステム

スコアやコンボ数はカスタムスクリプトで int/float として管理し、
`unity_gamekit_ui_binding` で UI 表示に連動させる。
一定時間内の連続クリアでコンボが増加する仕組みにする。

### 難易度スケーリング

各ステージの `moveLimit` と `timeLimit` を ScriptableObject に持たせ、
`unity_scriptableObject_crud(operation='update')` でランタイム外で調整できる。
A/B テストや QA フィードバックを素早く反映できる。

---

## 注意点・落とし穴

- **グリッドセルが多い場合**（100 個以上）は `unity_component_crud` の
  `addMultiple` 操作でコンポーネントを一括追加すること。個別呼び出しは低速になる。
- **Physics は使わない**: グリッドゲームで Rigidbody を使うと
  浮動小数点誤差でグリッドずれが発生する。Transform.position を整数グリッド座標で管理する。
- **Undo スタックのメモリ**: 全状態をスナップショット保存すると
  グリッドが大きいほどメモリを消費する。差分（Delta）方式を検討する。
- **ui_command ボタンの有効/無効**: Undo 可能なときだけボタンを有効にするには
  カスタムスクリプトから `unity_event_wiring` で接続したイベントを発火させる。
- **GameKit 生成スクリプト** は `unity_compilation_await` でコンパイルを待つ。
  create 操作後は必ず await してから次の操作を行うこと。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| データ | `unity_scriptableObject_crud` | ステージ・難易度データ |
| オブジェクト | `unity_gameobject_crud` | グリッドセル生成 |
| コンポーネント | `unity_component_crud` | Collider・SpriteRenderer 一括追加 |
| アセット | `unity_asset_crud` | ゲームロジックスクリプト作成 |
| 配置 | `unity_transform_batch` | セルの均等配置 |
| UI | `unity_gamekit_ui_command` | Undo・リセットボタン |
| UI | `unity_gamekit_ui_binding` | 手数・タイマー・スコア表示 |
| UI | `unity_gamekit_ui_list` | ステージ選択リスト |
| UI | `unity_gamekit_ui_selection` | 難易度タブ |
| UI基盤 | `unity_ui_foundation` | Canvas・Text 作成 |
| 演出 | `unity_gamekit_feedback` | クリア演出 |
| 演出 | `unity_gamekit_vfx` | 紙吹雪・クリアエフェクト |
| 演出 | `unity_gamekit_audio` | BGM・操作音 |
| イベント | `unity_event_wiring` | ボタン→スクリプト接続 |
| 設定 | `unity_projectSettings_crud` | ビルドシーン管理 |
| 検証 | `unity_validate_integrity` | 参照切れチェック |
| 検証 | `unity_scene_reference_graph` | オブジェクト間参照確認 |
