# 2Dシューター 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

縦スクロール・横スクロール・トップダウンなど多様なスタイルを含む。
核心となるのは「オブジェクトプーリング」「ウェーブ制スポーン」「スコア/コンボ管理」
「弾幕・プロジェクタイル処理」である。
ゲームロジック（弾の移動・衝突判定・スポーン制御・AI パターン）はカスタムスクリプトで実装し、
GameKit の Presentation Pillar（feedback, vfx, audio）でヒット演出を付加、
UI Pillar で HUD 表示を構築する。

---

## シーン構成

```
Scenes/
  Boot.unity            # 初期化
  MainMenu.unity        # タイトル・ランキング
  Game.unity            # メインゲームシーン
  BossStage.unity       # ボス専用シーン（任意）
  GameOver.unity        # ゲームオーバー・スコア表示
```

Game.unity の GameObject 構成例:

```
[Game Scene]
  - GameManager           # ウェーブ進行・フロー管理（カスタムスクリプト）
  - Player                # 自機
  - EnemyManager/         # 敵管理
  - BulletPool/           # 弾プール（カスタムスクリプト）
  |   - PlayerBullets
  |   - EnemyBullets
  - PowerUpManager/
  - FXManager/
  - FeedbackManager
  - UI/
  |   - Canvas_HUD        # HP・スコア・コンボ・ウェーブ
  - Audio/
  - Background/           # スクロール背景
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Player/
      PlayerShip.cs          # 手動作成: 自機移動・射撃制御
      PlayerAudio.cs         # 生成: unity_gamekit_audio
    Enemy/
      EnemyBase.cs           # 手動作成: 敵基底クラス
      EnemyMover.cs          # 手動作成: 移動パターン
      WaveSpawner.cs         # 手動作成: ウェーブ制スポーン
    Projectile/
      BulletPool.cs          # 手動作成: オブジェクトプーリング
      BulletBase.cs          # 手動作成: 弾基底クラス
    PowerUp/
      PowerUpItem.cs         # 手動作成: パワーアップ収集
    Score/
      ScoreManager.cs        # 手動作成: スコア・コンボ管理
    UI/
      HUDCommand.cs          # 生成: unity_gamekit_ui_command
      ScoreBinding.cs        # 生成: unity_gamekit_ui_binding
      ComboBinding.cs        # 生成: unity_gamekit_ui_binding
      HPBinding.cs           # 生成: unity_gamekit_ui_binding
  Data/
    Waves/
      Wave_Stage1.asset      # ScriptableObject: ウェーブ定義
    Enemies/
      Enemy_TypeA.asset      # ScriptableObject: 敵パラメータ
    PowerUps/
      Shield.asset           # ScriptableObject: パワーアップデータ
  Prefabs/
    Player.prefab
    Enemies/
    Bullets/
    PowerUps/
    FX/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: プロジェクト初期設定

```python
# 物理設定（2D シューター: 重力なし）
unity_projectSettings_crud(operation='write', category='physics2D',
    settings={'gravity': {'x': 0, 'y': 0}})

# タグ・レイヤー追加
unity_projectSettings_crud(operation='write', category='tags',
    settings={'tags': ['Player', 'Enemy', 'PlayerBullet', 'EnemyBullet', 'PowerUp']})

# Input Profile（シューター向け）
unity_input_profile(operation='createInputActions',
    assetName='ShooterInput',
    outputPath='Assets/Settings',
    actionMaps=[
        {'name': 'Player', 'actions': [
            {'name': 'Move', 'type': 'Value', 'valueType': 'Vector2',
             'bindings': [{'path': '<Gamepad>/leftStick'},
                          {'path': '<Keyboard>/arrows'}]},
            {'name': 'Fire', 'type': 'Button',
             'bindings': [{'path': '<Keyboard>/z'},
                          {'path': '<Gamepad>/buttonSouth'}]},
            {'name': 'Bomb', 'type': 'Button',
             'bindings': [{'path': '<Keyboard>/x'},
                          {'path': '<Gamepad>/buttonWest'}]},
        ]}
    ])
```

### Step 2: プレイヤー自機

```python
# 自機 GameObject
unity_gameobject_crud(operation='create', name='Player',
    position={'x': 0, 'y': -3, 'z': 0}, tag='Player')

# スプライト設定
unity_sprite2d_bundle(operation='createSprite', gameObjectPath='Player',
    spritePath='Assets/Sprites/Player/ship.png', pixelsPerUnit=16)

# 物理（kinematic: スクリプトで制御、小さな CircleCollider2D で当たり判定）
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'],
    preset='kinematic')

unity_component_crud(operation='add', gameObjectPath='Player',
    componentType='CircleCollider2D',
    properties={'radius': 0.1, 'isTrigger': true})

# 自機制御スクリプト作成
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Player/PlayerShip.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')
```

### Step 3: 弾プール設定

```python
# 弾プール親オブジェクト
unity_gameobject_crud(operation='create', name='BulletPool')
unity_gameobject_crud(operation='create', name='PlayerBullets',
    parentPath='BulletPool')
unity_gameobject_crud(operation='create', name='EnemyBullets',
    parentPath='BulletPool')

# 自弾プレハブ作成
unity_gameobject_crud(operation='create', name='PlayerBullet')
unity_sprite2d_bundle(operation='createSprite',
    gameObjectPath='PlayerBullet',
    spritePath='Assets/Sprites/Bullets/player_bullet.png', pixelsPerUnit=16)
unity_component_crud(operation='add', gameObjectPath='PlayerBullet',
    componentType='CircleCollider2D',
    properties={'radius': 0.15, 'isTrigger': true})
unity_prefab_crud(operation='create', gameObjectPath='PlayerBullet',
    prefabPath='Assets/Prefabs/Bullets/PlayerBullet.prefab')

# 敵弾プレハブ作成
unity_gameobject_crud(operation='create', name='EnemyBullet')
unity_sprite2d_bundle(operation='createSprite',
    gameObjectPath='EnemyBullet',
    spritePath='Assets/Sprites/Bullets/enemy_bullet.png', pixelsPerUnit=16)
unity_component_crud(operation='add', gameObjectPath='EnemyBullet',
    componentType='CircleCollider2D',
    properties={'radius': 0.1, 'isTrigger': true})
unity_prefab_crud(operation='create', gameObjectPath='EnemyBullet',
    prefabPath='Assets/Prefabs/Bullets/EnemyBullet.prefab')

# オブジェクトプールスクリプト作成
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Projectile/BulletPool.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')
```

### Step 4: ウェーブデータ（ScriptableObject）

```python
# ウェーブ定義データ
unity_scriptableObject_crud(operation='create',
    typeName='WaveData',
    assetPath='Assets/Data/Waves/Wave_Stage1.asset',
    fields={
        'waves': [
            {'waveId': 1, 'enemyCount': 5, 'enemyType': 'TypeA',
             'spawnInterval': 1.0},
            {'waveId': 2, 'enemyCount': 8, 'enemyType': 'TypeA',
             'spawnInterval': 0.8},
            {'waveId': 3, 'enemyCount': 3, 'enemyType': 'TypeB',
             'spawnInterval': 2.0},
        ]
    })

# 敵パラメータデータ
unity_scriptableObject_crud(operation='create',
    typeName='EnemyData',
    assetPath='Assets/Data/Enemies/Enemy_TypeA.asset',
    fields={
        'enemyName': 'Type A',
        'maxHP': 10,
        'moveSpeed': 3.0,
        'fireInterval': 2.0,
        'scoreValue': 100,
    })
```

### Step 5: HUD（スコア・コンボ・HP 表示）

```python
# HUD Canvas
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_HUD',
    renderMode='ScreenSpaceOverlay')

# スコア表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='ScoreText', text='Score: 0',
    position={'x': 300, 'y': 260})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/ScoreText',
    bindingId='score_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# コンボ表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='ComboText', text='Combo: 0',
    position={'x': 300, 'y': 230})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/ComboText',
    bindingId='combo_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# HP 表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='HPText', text='HP: 3',
    position={'x': -350, 'y': 260})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/HPText',
    bindingId='hp_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# ポーズ・リトライコマンド
unity_gamekit_ui_command(operation='createCommandPanel',
    panelId='pause_cmd',
    canvasPath='Canvas_HUD',
    commands=[
        {'name': 'Pause',  'commandType': 'action', 'label': 'ポーズ'},
        {'name': 'Retry',  'commandType': 'action', 'label': 'リトライ'},
    ])
unity_compilation_await(operation='await')
```

### Step 6: ヒット・撃破演出

```python
# FX マネージャ
unity_gameobject_crud(operation='create', name='FXManager')
unity_gameobject_crud(operation='create', name='FeedbackManager')

# 爆発 VFX
unity_gamekit_vfx(operation='create', targetPath='FXManager',
    vfxId='explosion_vfx')
unity_compilation_await(operation='await')

# ヒットスパーク VFX
unity_gamekit_vfx(operation='create', targetPath='FXManager',
    vfxId='hit_spark_vfx')
unity_compilation_await(operation='await')

# 被弾フィードバック（スクリーンシェイク + フラッシュ）
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='player_hit',
    components=[
        {'type': 'screenShake', 'intensity': 0.4, 'duration': 0.25},
        {'type': 'flash', 'color': {'r':1,'g':0,'b':0,'a':0.5}, 'duration': 0.1},
    ])
unity_compilation_await(operation='await')

# ボス撃破フィードバック（強め）
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='boss_defeat',
    components=[
        {'type': 'screenShake', 'intensity': 1.0, 'duration': 0.8},
    ])
unity_compilation_await(operation='await')
```

### Step 7: BGM・SE

```python
# BGM
unity_gameobject_crud(operation='create', name='Audio')
unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='game_bgm', audioClipPath='Assets/Audio/BGM/Game.mp3',
    loop=True)
unity_compilation_await(operation='await')

# 射撃SE
unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_shoot', audioClipPath='Assets/Audio/SFX/Shoot.wav')
unity_compilation_await(operation='await')

# 爆発SE
unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_explosion', audioClipPath='Assets/Audio/SFX/Explosion.wav')
unity_compilation_await(operation='await')
```

### Step 8: カメラ・背景

```python
# カメラ（固定）
unity_camera_rig(operation='createRig', rigType='fixed', rigName='MainCam',
    position={'x': 0, 'y': 0, 'z': -10})

# ビルドシーン登録
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Game.unity')

# 整合性検証
unity_validate_integrity(operation='all')
```

---

## よくあるパターン

### オブジェクトプーリングの実装

弾や敵の Instantiate/Destroy は毎フレーム呼ぶとフレームスパイクが発生するため、
カスタムスクリプトでオブジェクトプールを実装する。
`unity_prefab_crud(operation='instantiate')` で初期プール分を生成し、
非アクティブにして再利用する。プールサイズは敵弾 200 以上を推奨。

### コンボタイマーによるリセット

スコアマネージャのカスタムスクリプトで、敵撃破時にコンボカウンタを加算し、
一定時間撃破がなければコンボをリセットする。`unity_gamekit_ui_binding` で
コンボ数を UI にリアルタイム反映させる。

### ランキングシステム

ローカルランキングは PlayerPrefs またはカスタムスクリプトで JSON 保存し、
`unity_gamekit_ui_list` でランキング画面に表示する。

---

## 注意点・落とし穴

- **オブジェクトプーリング**: 弾・敵・VFX は必ずプーリングを使うこと。
  Instantiate/Destroy の繰り返しはフレームスパイクの原因になる。
- **物理レイヤー衝突マトリクス**: PlayerBullet と EnemyBullet が互いに衝突しないよう
  `unity_projectSettings_crud` で Layer Collision Matrix を設定すること。
- **弾の数上限**: プールサイズを超えて発射しようとすると弾が無視される。
  ボスの弾幕フェーズでは poolSize を多めに確保する。
- **スクロール背景**: スクロール背景の移動はスクリプトで行い、
  `Camera.main.transform` は動かさないこと（Camera Rig のシェイクと干渉する）。
- **GameKit 生成スクリプト** は create 操作後に `unity_compilation_await` を呼ぶこと。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| オブジェクト | `unity_gameobject_crud` | 自機・敵・弾プール作成 |
| コンポーネント | `unity_component_crud` | Collider2D 追加・設定 |
| アセット | `unity_asset_crud` | ゲームロジックスクリプト作成 |
| データ | `unity_scriptableObject_crud` | ウェーブ・敵パラメータ |
| プレハブ | `unity_prefab_crud` | 弾・敵・パワーアップのプレハブ化 |
| 物理 | `unity_physics_bundle` | kinematic 設定 |
| スプライト | `unity_sprite2d_bundle` | スプライト設定 |
| カメラ | `unity_camera_rig` | 固定カメラ |
| 入力 | `unity_input_profile` | 移動・射撃・ボム入力 |
| UI | `unity_gamekit_ui_binding` | スコア・コンボ・HP 表示 |
| UI | `unity_gamekit_ui_command` | ポーズ・リトライ |
| UI | `unity_gamekit_ui_list` | ランキング表示 |
| UI基盤 | `unity_ui_foundation` | Canvas・Text 作成 |
| 演出 | `unity_gamekit_vfx` | 爆発・ヒットスパーク |
| 演出 | `unity_gamekit_feedback` | 被弾シェイク・ボス撃破 |
| 演出 | `unity_gamekit_audio` | BGM・SE |
| イベント | `unity_event_wiring` | UnityEvent 接続 |
| 設定 | `unity_projectSettings_crud` | 物理・レイヤー設定 |
| 検証 | `unity_validate_integrity` | プール参照切れ検出 |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
