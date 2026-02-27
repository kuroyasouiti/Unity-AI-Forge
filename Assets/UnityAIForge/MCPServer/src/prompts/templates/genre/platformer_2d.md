# 2Dプラットフォーマー 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

横スクロール・縦スクロール問わず、ジャンプとフィールド踏破が基本となるジャンル。
Tilemap による地形構築、Rigidbody2D による物理演算、アニメーション同期が実装の三本柱となる。
GameKit の Presentation Pillar（animation_sync, feedback, vfx, audio）と Mid-Level の 2D ツール群を組み合わせ、
ゲームロジック部分は `unity_component_crud` でコンポーネントを追加し、
`unity_asset_crud` でカスタムスクリプトを作成して実装する。

---

## シーン構成

```
Scenes/
  Boot.unity          # 初期化・ローディング
  MainMenu.unity      # タイトル画面
  World_1_1.unity     # プレイシーン（ステージ単位）
  World_1_2.unity
  GameOver.unity
  StageClear.unity
```

各プレイシーンの GameObject 構成例:

```
[Scene Root]
  - Environment/
  |   - Tilemap_Ground     (Tilemap + TilemapCollider2D + CompositeCollider2D)
  |   - Tilemap_Background (Tilemap, no physics)
  |   - Tilemap_Hazard     (Tilemap + TilemapCollider2D, trigger)
  - Player
  |   - SpriteRenderer + Animator
  |   - Rigidbody2D + CapsuleCollider2D
  - Enemies/
  |   - Enemy_Goomba (Prefab)
  - Items/
  |   - Coin (Prefab), Mushroom (Prefab)
  - UI/
  |   - Canvas_HUD (HP, Score, Timer, Lives)
  - Audio/
  |   - BGM, SFX
  - Camera
  - GameManager (Empty + カスタムスクリプト)
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Player/
      PlayerController.cs      # 手動作成 or unity_asset_crud で生成
      PlayerAnimationSync.cs   # 生成: unity_gamekit_animation_sync
      PlayerAudio.cs           # 生成: unity_gamekit_audio
    Enemy/
      EnemyController.cs       # 手動作成（移動・衝突判定スクリプト）
    Items/
      CoinCollectible.cs       # 手動作成（OnTriggerEnter2D ベース）
    UI/
      HUDCommand.cs            # 生成: unity_gamekit_ui_command
      HPBinding.cs             # 生成: unity_gamekit_ui_binding
      ScoreBinding.cs          # 生成: unity_gamekit_ui_binding
    Game/
      GameManager.cs           # 手動作成（ゲームフロー制御）
  Data/
    StageData/
      Stage_1_1.asset          # ScriptableObject: ステージ設定
  Prefabs/
    Player.prefab
    Enemies/
    Items/
  Tilemaps/
    Tileset_Forest.asset
  Audio/
    BGM/, SFX/
  Sprites/
    Player/, Enemies/, Items/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: シーン・プロジェクト初期設定

```python
# プロジェクト設定: 2D物理・重力設定
unity_projectSettings_crud(operation='write', category='physics2D',
    settings={'gravity': {'x': 0, 'y': -20}})

# Input Profile（プラットフォーマー向け）
unity_input_profile(operation='createInputActions',
    assetName='PlatformerInput',
    outputPath='Assets/Settings',
    actionMaps=[
        {'name': 'Player', 'actions': [
            {'name': 'Move', 'type': 'Value', 'valueType': 'Vector2',
             'bindings': [{'path': '<Gamepad>/leftStick'}, {'path': '<Keyboard>/ad'}]},
            {'name': 'Jump', 'type': 'Button',
             'bindings': [{'path': '<Keyboard>/space'}, {'path': '<Gamepad>/buttonSouth'}]},
        ]}
    ])

# プレイシーン作成
unity_scene_crud(operation='create', sceneName='World_1_1',
    scenePath='Assets/Scenes/World_1_1.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/World_1_1.unity')
```

### Step 2: Tilemap 地形構築

```python
# 親オブジェクト作成
unity_gameobject_crud(operation='create', name='Environment')

# Tilemap セットアップ（地形・背景・ハザード）
unity_tilemap_bundle(operation='createTilemap', tilemapName='Tilemap_Ground',
    parentPath='Environment', hasTilemapCollider=True, useCompositeCollider=True)

unity_tilemap_bundle(operation='createTilemap', tilemapName='Tilemap_Background',
    parentPath='Environment', hasTilemapCollider=False)

unity_tilemap_bundle(operation='createTilemap', tilemapName='Tilemap_Hazard',
    parentPath='Environment', hasTilemapCollider=True, isTrigger=True)

# ルールタイル作成
unity_tilemap_bundle(operation='createRuleTile', tileName='ForestGround',
    spritePath='Assets/Sprites/Tileset_Forest.png',
    outputPath='Assets/Tilemaps')

# タイル配置
unity_tilemap_bundle(operation='setTiles',
    tilemapPath='Environment/Tilemap_Ground',
    tiles=[
        {'position': {'x': i, 'y': 0, 'z': 0}, 'tileName': 'ForestGround'}
        for i in range(20)
    ])
```

### Step 3: プレイヤーセットアップ

```python
# プレイヤー GameObject 作成
unity_gameobject_crud(operation='create', name='Player',
    position={'x': 2, 'y': 3, 'z': 0})

# スプライト設定
unity_sprite2d_bundle(operation='createSprite', gameObjectPath='Player',
    spritePath='Assets/Sprites/Player/player_sheet.png',
    pixelsPerUnit=16)

# アニメーションコントローラ作成
unity_animation2d_bundle(operation='createController',
    controllerName='PlayerAnimator',
    outputPath='Assets/Animations/Player')

# アニメーションステート追加
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Idle', clipPath='Assets/Animations/Player/Idle.anim', isDefault=True)
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Run', clipPath='Assets/Animations/Player/Run.anim')
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Jump', clipPath='Assets/Animations/Player/Jump.anim')

# 物理設定（Rigidbody2D + CapsuleCollider2D をプラットフォーマー向けに最適化）
unity_component_crud(operation='add', gameObjectPath='Player',
    componentType='Rigidbody2D',
    propertyChanges={'gravityScale': 3, 'mass': 1,
        'collisionDetection': 'Continuous',
        'constraints': {'freezeRotationZ': True}})
unity_component_crud(operation='add', gameObjectPath='Player',
    componentType='CapsuleCollider2D',
    propertyChanges={'size': {'x': 0.8, 'y': 1.2}, 'direction': 1})

# Animator をアタッチ
unity_animation2d_bundle(operation='setupAnimator', gameObjectPath='Player',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller')

# アニメーション同期（速度・接地判定を自動でパラメータに反映）
unity_gamekit_animation_sync(operation='create', targetPath='Player',
    syncId='player_anim', syncSource='rigidbody2d', animatorPath='Player')
unity_compilation_await(operation='await')

unity_gamekit_animation_sync(operation='addSyncRule', syncId='player_anim',
    parameterName='Speed', sourceField='velocity.magnitude')
unity_gamekit_animation_sync(operation='addSyncRule', syncId='player_anim',
    parameterName='VelocityY', sourceField='velocity.y')
```

### Step 4: カメラ・HUD

```python
# フォローカメラ（プレイヤー追尾）
unity_camera_rig(operation='createRig', rigType='follow', rigName='MainCam',
    targetPath='Player',
    offset={'x': 0, 'y': 2, 'z': -10},
    smoothSpeed=5.0)

# HUD Canvas
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_HUD',
    renderMode='ScreenSpaceOverlay')

# HP バー: ui_binding で表示を連動
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='HPText', text='HP: 3', position={'x': -350, 'y': 250})

unity_gamekit_ui_binding(operation='create', targetPath='Canvas_HUD/HPText',
    bindingId='player_hp', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# スコア表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='ScoreText', text='Score: 0', position={'x': 350, 'y': 250})

unity_gamekit_ui_binding(operation='create', targetPath='Canvas_HUD/ScoreText',
    bindingId='score_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')
```

### Step 5: コイン・アイテム収集

```python
# コインオブジェクト作成
unity_gameobject_crud(operation='create', name='Coin',
    position={'x': 5, 'y': 4, 'z': 0})

# スプライト設定
unity_sprite2d_bundle(operation='createSprite', gameObjectPath='Coin',
    spritePath='Assets/Sprites/Items/coin.png', pixelsPerUnit=16)

# 収集判定用の Collider2D（Trigger モード）
unity_component_crud(operation='add', gameObjectPath='Coin',
    componentType='CircleCollider2D',
    properties={'isTrigger': true})

# プレハブ化
unity_prefab_crud(operation='create', gameObjectPath='Coin',
    prefabPath='Assets/Prefabs/Items/Coin.prefab')
```

### Step 6: 敵の作成

```python
# 敵 GameObject
unity_gameobject_crud(operation='create', name='Enemy_Goomba',
    parentPath='Enemies', position={'x': 10, 'y': 2, 'z': 0})

# スプライト・物理・コライダー
unity_sprite2d_bundle(operation='createSprite',
    gameObjectPath='Enemies/Enemy_Goomba',
    spritePath='Assets/Sprites/Enemies/goomba.png', pixelsPerUnit=16)

unity_component_crud(operation='add',
    gameObjectPath='Enemies/Enemy_Goomba', componentType='Rigidbody2D',
    propertyChanges={'gravityScale': 2, 'mass': 1,
        'collisionDetection': 'Continuous',
        'constraints': {'freezeRotationZ': True}})
unity_component_crud(operation='add',
    gameObjectPath='Enemies/Enemy_Goomba', componentType='BoxCollider2D',
    propertyChanges={'size': {'x': 0.9, 'y': 0.9}})

# プレハブ化して複数配置
unity_prefab_crud(operation='create',
    gameObjectPath='Enemies/Enemy_Goomba',
    prefabPath='Assets/Prefabs/Enemies/Enemy_Goomba.prefab')
```

### Step 7: ダメージフィードバック・演出

```python
# 被弾フィードバック（ヒットストップ＋スクリーンシェイク）
unity_gameobject_crud(operation='create', name='FeedbackManager')
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='player_hit',
    components=[
        {'type': 'hitstop', 'duration': 0.06},
        {'type': 'screenShake', 'intensity': 0.25, 'duration': 0.2},
    ])
unity_compilation_await(operation='await')

# 死亡 VFX
unity_gameobject_crud(operation='create', name='FX', parentPath='')
unity_gamekit_vfx(operation='create', targetPath='FX',
    vfxId='player_death_vfx')
unity_compilation_await(operation='await')

# BGM
unity_gameobject_crud(operation='create', name='BGM', parentPath='Audio')
unity_gamekit_audio(operation='create', targetPath='Audio/BGM',
    audioId='stage_bgm', audioClipPath='Assets/Audio/BGM/Stage1.mp3',
    loop=True, volume=0.8)
unity_compilation_await(operation='await')
```

### Step 8: ステージ進行・シーン遷移設定

```python
# ゴールオブジェクト
unity_gameobject_crud(operation='create', name='GoalFlag',
    position={'x': 30, 'y': 3, 'z': 0})

# ゴールのコライダー（トリガー）
unity_component_crud(operation='add', gameObjectPath='GoalFlag',
    componentType='BoxCollider2D',
    properties={'isTrigger': true, 'size': {'x': 1, 'y': 3}})

# シーンをビルドに追加
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/World_1_1.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/World_1_2.unity')

# シーン遷移の整合性を検証
unity_scene_relationship_graph(operation='analyzeAll')
```

---

## よくあるパターン

### 接地判定の実装

`unity_component_crud` で Rigidbody2D（gravityScale=3, Continuous衝突検出）+ CapsuleCollider2D が設定される。
接地判定は Raycast または足元の小さな BoxCast で実装する。
`animation_sync` の `addSyncRule` で `isGrounded` を `Bool` パラメータにバインドすれば
ジャンプアニメーションへの遷移が自動化される。

### ステージ遷移の実装

ゴールオブジェクトに `BoxCollider2D(isTrigger=true)` を付け、
カスタムスクリプトの `OnTriggerEnter2D` で `SceneManager.LoadScene()` を呼ぶ。
遷移先シーンは ScriptableObject にデータとして持たせるとステージ構成の変更に強い。

### 難易度調整

敵のスポーン間隔・最大数・アイテムドロップ率などを ScriptableObject に持たせ、
`unity_scriptableObject_crud(operation='update')` で Play モードに入らず数値調整できる。

---

## 注意点・落とし穴

- **Tilemap の CompositeCollider2D** は GeometryType を `Polygons` にすること。
  `Outlines` だと天井や坂でスタックが発生しやすい。
- **Rigidbody2D の Collision Detection** は `Continuous` に設定する。
  高速移動時に壁をすり抜ける「トンネリング」を防ぐ。
- **animation_sync** は Animator がアクティブな GameObject に作成すること。
  非アクティブ時は `syncSource` の値取得が失敗する。
- **GameKit 生成スクリプト** は `unity_compilation_await` で
  コンパイル完了を待ってからプロパティ設定を行うこと。
- **Prefab の変更** は `unity_prefab_crud(operation='applyOverrides')` を忘れずに。
  シーン上の変更はプレハブに反映されなければ消える。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| 地形 | `unity_tilemap_bundle` | Tilemap 作成・タイル配置 |
| 地形 | `unity_sprite2d_bundle` | スプライト設定・スライス |
| アニメーション | `unity_animation2d_bundle` | Animator・クリップ生成 |
| アニメーション | `unity_gamekit_animation_sync` | 速度→アニメパラメータ同期 |
| 物理 | `unity_component_crud` | Rigidbody2D・Collider2D 追加・物理設定 |
| カメラ | `unity_camera_rig` | フォローカメラ設定 |
| オブジェクト | `unity_gameobject_crud` | GameObject 作成・配置 |
| コンポーネント | `unity_component_crud` | Collider・Rigidbody 追加 |
| アセット | `unity_asset_crud` | スクリプトファイル作成 |
| データ | `unity_scriptableObject_crud` | ステージ設定データ |
| プレハブ | `unity_prefab_crud` | 敵・アイテムのプレハブ化 |
| UI | `unity_ui_foundation` | HUD Canvas 構築 |
| UI | `unity_gamekit_ui_binding` | HP・スコア表示バインド |
| UI | `unity_gamekit_ui_command` | ポーズ・リトライ操作 |
| 演出 | `unity_gamekit_feedback` | ヒットストップ・シェイク |
| 演出 | `unity_gamekit_vfx` | 爆発・死亡エフェクト |
| 演出 | `unity_gamekit_audio` | BGM・SE 再生 |
| 入力 | `unity_input_profile` | Input System 設定 |
| イベント | `unity_event_wiring` | UnityEvent 接続 |
| 設定 | `unity_projectSettings_crud` | 物理・タグ・ビルド設定 |
| 検証 | `unity_validate_integrity` | Missing Script 検出 |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
