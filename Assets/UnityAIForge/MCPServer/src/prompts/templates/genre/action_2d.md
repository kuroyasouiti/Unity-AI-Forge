# 2Dアクションゲーム 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

近接戦闘（コンボ・パリィ・回避）、敵 AI（巡回・追跡・攻撃）、ヒット演出（ヒットストップ・スクリーンシェイク）が
ゲームフィールの核心となるジャンル。
ゲームロジック（コンバットシステム・敵 AI・ヒットボックス判定）はカスタムスクリプトで実装し、
GameKit の Presentation Pillar（animation_sync, feedback, vfx, audio）で戦闘の手触りを演出、
Mid-Level ツール（physics_bundle, tilemap_bundle, animation2d_bundle）で基盤を構築する。
メトロイドヴァニア・ローグライクアクションにも応用可能。

---

## シーン構成

```
Scenes/
  Boot.unity          # 初期化
  MainMenu.unity      # タイトル
  Town.unity          # 街・拠点（セーブポイント）
  Dungeon_01.unity    # ダンジョン 1（複数）
  Boss_01.unity       # ボス部屋
  GameOver.unity
```

Dungeon シーンの GameObject 構成例:

```
[Dungeon Scene]
  - Player
  |   - SpriteRenderer + Animator
  |   - Rigidbody2D + CapsuleCollider2D
  |   - HitBox (攻撃判定: BoxCollider2D, Trigger)
  |   - HurtBox (被弾判定: BoxCollider2D, Trigger)
  - Enemies/
  |   - Enemy_Skeleton (Prefab x N)
  |   - Enemy_Archer
  - Environment/
  |   - Tilemap_Ground
  |   - Tilemap_Walls
  |   - Tilemap_Hazard
  - FXManager/
  - FeedbackManager
  - UI/
  |   - Canvas_HUD      # HP・スタミナ・コンボ
  - Audio/
  - Camera              # フォロー
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Player/
      PlayerController.cs    # 手動作成: 移動・ジャンプ・回避
      PlayerCombat.cs        # 手動作成: 攻撃コンボ・ヒットボックス制御
      PlayerAnimSync.cs      # 生成: unity_gamekit_animation_sync
      PlayerAudio.cs         # 生成: unity_gamekit_audio
    Enemy/
      EnemyBase.cs           # 手動作成: 敵基底クラス
      EnemyAI.cs             # 手動作成: 巡回・追跡・攻撃 AI
      EnemyAnimSync.cs       # 生成: unity_gamekit_animation_sync
    Combat/
      HitboxManager.cs       # 手動作成: ヒットボックス有効/無効制御
      DamageCalculator.cs    # 手動作成: ダメージ計算
    Presentation/
      HitFeedback.cs         # 生成: unity_gamekit_feedback
      HitVFX.cs              # 生成: unity_gamekit_vfx
    UI/
      HUDBinding.cs          # 生成: unity_gamekit_ui_binding
  Data/
    Characters/
      Player_Data.asset      # ScriptableObject: プレイヤーパラメータ
      Enemy_Skeleton.asset   # ScriptableObject: 敵パラメータ
  Prefabs/
    Player.prefab
    Enemies/
    FX/
  Sprites/
    Player/, Enemies/
  Animations/
    Player/, Enemies/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: 地形・物理設定

```python
# 2D 物理（アクション向け: 重力強め）
unity_projectSettings_crud(operation='write', category='physics2D',
    settings={'gravity': {'x': 0, 'y': -25}})

# タグ・レイヤー
unity_projectSettings_crud(operation='write', category='tags',
    settings={'tags': ['Player', 'Enemy', 'HitBox', 'HurtBox']})

# Tilemap 地形（地面・壁）
unity_gameobject_crud(operation='create', name='Environment')

unity_tilemap_bundle(operation='createTilemap', tilemapName='Tilemap_Ground',
    parentPath='Environment', hasTilemapCollider=True,
    useCompositeCollider=True)

unity_tilemap_bundle(operation='createTilemap', tilemapName='Tilemap_Walls',
    parentPath='Environment', hasTilemapCollider=True,
    useCompositeCollider=True)
```

### Step 2: プレイヤーセットアップ

```python
# プレイヤー GameObject
unity_gameobject_crud(operation='create', name='Player',
    position={'x': 0, 'y': 2, 'z': 0}, tag='Player')

# スプライト
unity_sprite2d_bundle(operation='createSprite', gameObjectPath='Player',
    spritePath='Assets/Sprites/Player/hero_sheet.png', pixelsPerUnit=16)

# アニメーションコントローラ作成とステート追加
unity_animation2d_bundle(operation='createController',
    controllerName='PlayerAnimator',
    outputPath='Assets/Animations/Player')

unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Idle', clipPath='Assets/Animations/Player/Idle.anim',
    isDefault=True)
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Run', clipPath='Assets/Animations/Player/Run.anim')
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Attack_01', clipPath='Assets/Animations/Player/Attack_01.anim')
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Attack_02', clipPath='Assets/Animations/Player/Attack_02.anim')
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    stateName='Hit', clipPath='Assets/Animations/Player/Hit.anim')

# トランジション追加
unity_animation2d_bundle(operation='addTransition',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller',
    sourceState='Idle', destinationState='Run',
    conditionParameter='Speed', conditionMode='Greater', conditionThreshold=0.1)

# 物理（platformer プリセット）
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'],
    preset='platformer')

# Animator 設定
unity_animation2d_bundle(operation='setupAnimator', gameObjectPath='Player',
    controllerPath='Assets/Animations/Player/PlayerAnimator.controller')

# アニメーション同期（速度・イベントを自動同期）
unity_gamekit_animation_sync(operation='create', targetPath='Player',
    syncId='player_anim', syncSource='rigidbody2d', animatorPath='Player')
unity_compilation_await(operation='await')

unity_gamekit_animation_sync(operation='addSyncRule', syncId='player_anim',
    parameterName='Speed', sourceField='velocity.magnitude')
unity_gamekit_animation_sync(operation='addSyncRule', syncId='player_anim',
    parameterName='VelocityY', sourceField='velocity.y')
```

### Step 3: ヒットボックス・ハートボックス

```python
# 攻撃判定用子オブジェクト（HitBox）
unity_gameobject_crud(operation='create', name='HitBox',
    parentPath='Player',
    position={'x': 0.8, 'y': 0, 'z': 0})

unity_component_crud(operation='add', gameObjectPath='Player/HitBox',
    componentType='BoxCollider2D',
    properties={'size': {'x': 1.2, 'y': 1.0}, 'isTrigger': true})

# HitBox は通常非アクティブ（攻撃時のみスクリプトで有効化）
unity_gameobject_crud(operation='update', name='HitBox',
    path='Player/HitBox', active=false)

# 被弾判定用子オブジェクト（HurtBox）
unity_gameobject_crud(operation='create', name='HurtBox',
    parentPath='Player')

unity_component_crud(operation='add', gameObjectPath='Player/HurtBox',
    componentType='BoxCollider2D',
    properties={'size': {'x': 0.6, 'y': 1.4}, 'isTrigger': true})

# コンバットスクリプト作成
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Player/PlayerCombat.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')
```

### Step 4: ヒット演出（ヒットストップ・スクリーンシェイク・VFX）

```python
unity_gameobject_crud(operation='create', name='FeedbackManager')
unity_gameobject_crud(operation='create', name='FXManager')

# 通常攻撃ヒットフィードバック
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='hit_normal',
    components=[
        {'type': 'hitstop', 'duration': 0.05},
        {'type': 'screenShake', 'intensity': 0.15, 'duration': 0.12},
    ])
unity_compilation_await(operation='await')

# フィニッシャー（コンボ締め）
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='hit_finisher',
    components=[
        {'type': 'hitstop', 'duration': 0.12},
        {'type': 'screenShake', 'intensity': 0.4, 'duration': 0.3},
    ])
unity_compilation_await(operation='await')

# 斬撃 VFX
unity_gamekit_vfx(operation='create', targetPath='FXManager',
    vfxId='slash_vfx')
unity_compilation_await(operation='await')

# ヒットスパーク VFX
unity_gamekit_vfx(operation='create', targetPath='FXManager',
    vfxId='hit_vfx')
unity_compilation_await(operation='await')
```

### Step 5: 敵セットアップ

```python
# 敵パラメータ（ScriptableObject）
unity_scriptableObject_crud(operation='create',
    typeName='EnemyData',
    assetPath='Assets/Data/Characters/Enemy_Skeleton.asset',
    fields={
        'enemyName': 'Skeleton',
        'maxHP': 30,
        'attack': 8,
        'defense': 3,
        'moveSpeed': 2.0,
        'detectionRadius': 5.0,
        'attackRadius': 1.2,
    })

# スケルトン敵
unity_gameobject_crud(operation='create', name='Enemy_Skeleton',
    parentPath='Enemies', position={'x': 5, 'y': 2, 'z': 0}, tag='Enemy')

unity_sprite2d_bundle(operation='createSprite',
    gameObjectPath='Enemies/Enemy_Skeleton',
    spritePath='Assets/Sprites/Enemies/skeleton_sheet.png', pixelsPerUnit=16)

unity_physics_bundle(operation='applyPreset2D',
    gameObjectPaths=['Enemies/Enemy_Skeleton'], preset='character')

# 敵アニメーション
unity_animation2d_bundle(operation='setupAnimator',
    gameObjectPath='Enemies/Enemy_Skeleton',
    controllerPath='Assets/Animations/Enemies/SkeletonAnimator.controller')

# 敵アニメーション同期
unity_gamekit_animation_sync(operation='create',
    targetPath='Enemies/Enemy_Skeleton',
    syncId='skeleton_anim', syncSource='rigidbody2d',
    animatorPath='Enemies/Enemy_Skeleton')
unity_compilation_await(operation='await')

unity_gamekit_animation_sync(operation='addSyncRule', syncId='skeleton_anim',
    parameterName='Speed', sourceField='velocity.magnitude')

# プレハブ化
unity_prefab_crud(operation='create',
    gameObjectPath='Enemies/Enemy_Skeleton',
    prefabPath='Assets/Prefabs/Enemies/Enemy_Skeleton.prefab')
```

### Step 6: HUD・カメラ

```python
# HP バー
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_HUD',
    renderMode='ScreenSpaceOverlay')

unity_ui_foundation(operation='createText', canvasPath='Canvas_HUD',
    textName='HPBar', text='HP: 100/100',
    position={'x': -300, 'y': 250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_HUD/HPBar',
    bindingId='player_hp', uiType='slider', format='ratio')
unity_compilation_await(operation='await')

# フォローカメラ（デッドゾーン付き）
unity_camera_rig(operation='createRig', rigType='follow', rigName='MainCam',
    targetPath='Player',
    offset={'x': 0, 'y': 1, 'z': -10},
    smoothSpeed=6.0)
```

### Step 7: BGM・SE

```python
unity_gameobject_crud(operation='create', name='Audio')

unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='dungeon_bgm', audioClipPath='Assets/Audio/BGM/Dungeon.mp3',
    loop=True)
unity_compilation_await(operation='await')

unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_slash', audioClipPath='Assets/Audio/SFX/Slash.wav')
unity_compilation_await(operation='await')

unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_hit', audioClipPath='Assets/Audio/SFX/HitEnemy.wav')
unity_compilation_await(operation='await')
```

### Step 8: 検証・ビルド設定

```python
# シーンをビルドに追加
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Dungeon_01.unity')

# 参照の整合性検証
unity_validate_integrity(operation='all')

# シーン内参照グラフ
unity_scene_reference_graph(operation='analyzeScene')
```

---

## よくあるパターン

### コンボシステムの状態遷移

カスタムスクリプトでコンボタイマー（float カウントダウン）を実装する。
タイマーが切れたらコンボカウンタをリセット。
Attack_01 → Attack_02 → Attack_03 の連携は Animator の遷移条件で
`animation_sync` の `addTriggerRule` と組み合わせて制御する。

### パリィ・ジャストガード

カスタムスクリプトでパリィウィンドウ（数フレームの判定期間）を管理する。
パリィ成功時は `unity_gamekit_feedback` でフィードバックを発火し、
敵をヒットストップ状態にしながらカウンター攻撃に移行する。

### 部屋制ダンジョン（敵全滅で扉が開く）

部屋内の敵数をカスタムスクリプトでカウントし、全滅時にイベントを発火する。
`unity_event_wiring` で敵全滅イベント → ドア Animator の Open トリガーをつなぐ。

---

## 注意点・落とし穴

- **ヒットストップ中の物理**: `hitstop` は `Time.timeScale` を一時的に 0 に近づけるため、
  Rigidbody2D が静止する。ヒットストップ中にプレイヤー入力を受け付けないよう
  Input をガードすること。
- **ヒットボックスの有効フレーム**: HitBox は攻撃アニメーションの特定フレームのみ有効にする。
  常時有効だと当たり判定が広すぎる。AnimationEvent でスクリプトの有効/無効を切り替える。
- **敵 AI のパフォーマンス**: 敵が 10 体以上いる場合、AI の更新頻度を
  下げることを検討する（例: 0.1 秒間隔のコルーチン）。
- **animation_sync の同期先**: `syncSource='rigidbody2d'` は Rigidbody2D の velocity を参照する。
  敵が Kinematic の場合は `syncSource='transform'` を使う。
- **GameKit 生成スクリプト** は `unity_compilation_await` でコンパイルを待つ。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| 地形 | `unity_tilemap_bundle` | ダンジョン地形 |
| オブジェクト | `unity_gameobject_crud` | プレイヤー・敵・HitBox 作成 |
| コンポーネント | `unity_component_crud` | Collider2D 追加・設定 |
| アセット | `unity_asset_crud` | コンバット・AI スクリプト作成 |
| データ | `unity_scriptableObject_crud` | キャラクターパラメータ |
| プレハブ | `unity_prefab_crud` | 敵プレハブ化 |
| アニメーション | `unity_animation2d_bundle` | クリップ・Controller 生成 |
| アニメーション | `unity_gamekit_animation_sync` | 速度・イベント同期 |
| 物理 | `unity_physics_bundle` | platformer/character 設定 |
| スプライト | `unity_sprite2d_bundle` | スプライト設定 |
| カメラ | `unity_camera_rig` | フォローカメラ |
| 演出 | `unity_gamekit_feedback` | ヒットストップ・シェイク |
| 演出 | `unity_gamekit_vfx` | 斬撃・ヒットエフェクト |
| 演出 | `unity_gamekit_audio` | BGM・SE |
| UI | `unity_gamekit_ui_binding` | HP・スタミナ表示 |
| UI基盤 | `unity_ui_foundation` | Canvas・Text 作成 |
| イベント | `unity_event_wiring` | ドア開放・イベント接続 |
| 設定 | `unity_projectSettings_crud` | 物理・タグ設定 |
| 検証 | `unity_validate_integrity` | Missing Script 検出 |
| 検証 | `unity_scene_reference_graph` | 参照グラフ分析 |
