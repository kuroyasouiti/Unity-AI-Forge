# Unity-AI-Forge MCP Server v{VERSION} - Quick Reference

AI駆動型Unity開発ツールキット。MCPサーバー + GameKitフレームワーク。3層構造（Low/Mid/High-Level）で効率的な開発を実現。

## 🔴 Critical Rules (必ず守る)

1. .metaファイルは絶対に編集しない（Unity自動管理、手動編集は参照破壊）
2. 全Unity操作にMCPツール（unity_*）を使用
3. 変更前に operation='inspect' で対象を確認
4. **ツール優先順位: High-Level → Mid-Level → Low-Level** の順で選択（下記参照）
5. コンパイルが必要な操作は自動待機（ブリッジ再接続で解除）
6. **UI優先設計**: 人間が操作・確認できるUIから優先的に実装する（下記参照）
7. **シーン分割**: 機能ごとにシーンを分ける（下記参照）

## 🎯 UI優先設計原則 (Human-First UI Design)

ゲーム開発では、人間が操作・デバッグできるUIを最初に作成することで、開発効率と品質が大幅に向上します。

### なぜUI優先か？

1. **即座のフィードバック**: UIがあればゲームの状態を視覚的に確認できる
2. **手動テスト**: AIが作成したロジックを人間が手動でテスト可能
3. **デバッグ容易**: 問題発生時にUIからゲーム状態を確認・操作できる
4. **イテレーション高速化**: パラメータ調整をUI経由でリアルタイムに行える

### 推奨実装順序

```
1. Canvas/UI構造 → unity_ui_foundation
2. デバッグUI（ステータス表示、ログ表示）
3. 操作UI（ボタン、スライダー）→ unity_gamekit_ui_command
4. ゲームロジック → unity_gamekit_actor, unity_gamekit_manager
5. インタラクション → unity_gamekit_interaction
```

### UI優先の実装例

```python
# ❌ 悪い例: ロジックを先に作り、UIは後回し
# 1. プレイヤーアクター作成
# 2. 敵AI作成
# 3. 戦闘ロジック実装
# 4. UI作成（最後）

# ✅ 良い例: UIを先に作り、ロジックは後
# 1. Canvas作成
unity_ui_foundation(operation='createCanvas', name='GameUI')

# 2. ステータス表示UI
unity_ui_foundation(operation='createText', parentPath='GameUI', name='HPText', text='HP: 100/100')
unity_ui_foundation(operation='createText', parentPath='GameUI', name='MPText', text='MP: 50/50')

# 3. 操作ボタンUI（GameKitUICommand）
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

# 4. その後でゲームロジック実装
unity_gamekit_actor(operation='create', actorId='player', behaviorProfile='2dPhysics', controlMode='directController')
```

### デバッグUI推奨パターン

- **リソース表示**: HP/MP/Gold等の現在値をテキストで表示
- **ステート表示**: 現在のゲームフェーズ/ターンを表示
- **操作ボタン**: 手動でターン進行、リソース追加/消費をテスト
- **ログパネル**: イベント発生時のログ表示

## 🎬 シーン分割原則 (Scene Separation)

Unityプロジェクトでは、機能ごとにシーンを分割することで保守性と再利用性が向上します。

### なぜシーン分割か？

1. **並行開発**: 複数人が同時に異なるシーンで作業可能
2. **メモリ効率**: 必要なシーンのみロードしてメモリ節約
3. **テスト容易**: 個別シーンを単独でテスト可能
4. **再利用性**: UI/Audio/Managerシーンを複数レベルで共有
5. **ビルド最適化**: 不要なシーンを除外してビルドサイズ削減

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
│   ├── Level2.unity
│   └── ...
└── Debug/
    └── TestScene.unity  # デバッグ用
```

### シーン分割の実装例

```python
# 1. ブートシーンでマネージャー初期化
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Boot.unity')

# 2. GameKitSceneFlowでシーン遷移を定義
unity_gamekit_sceneflow(
    operation='create',
    flowId='MainFlow',
)
unity_gamekit_sceneflow(
    operation='addScene',
    flowId='MainFlow',
    sceneName='Title',
    scenePath='Assets/Scenes/Title.unity',
    loadMode='single'
)
unity_gamekit_sceneflow(
    operation='addScene',
    flowId='MainFlow',
    sceneName='Level1',
    scenePath='Assets/Scenes/Levels/Level1.unity',
    loadMode='single',
    sharedScenePaths=['Assets/Scenes/GameUI.unity', 'Assets/Scenes/AudioManager.unity']
)

# 3. シーン間遷移を定義
unity_gamekit_sceneflow(
    operation='addTransition',
    flowId='MainFlow',
    fromScene='Title',
    toScene='Level1',
    trigger='StartGame'
)
```

### シーンタイプ別ガイド

| シーンタイプ | loadMode | 用途 |
|------------|----------|------|
| Boot | single | 起動時初期化、GameManagerなど |
| Menu/Title | single | 画面単位の切り替え |
| Level | single | ゲームプレイ本体 |
| UI Overlay | additive | 複数レベルで共有するUI |
| Audio | additive | BGM/SE管理（DontDestroyOnLoad） |
| Debug | single | テスト・デバッグ用 |

## 🏗️ 3層アーキテクチャ

### Low-Level CRUD (8ツール) - 基本操作

Scene, GameObject, Component, Asset, ScriptableObject, Prefab, VectorSprite, ProjectSettings
用途: 詳細な制御、単一オブジェクト操作、プロパティ精密設定

### Mid-Level Batch (14ツール) - バッチ操作とプリセット

Transform, RectTransform, Physics, Camera, UI Foundation, Audio, Input, CharacterController, Tilemap, Sprite2D, Animation2D, UI Hierarchy, UI State, UI Navigation
用途: 複数オブジェクト一括処理、プリセット適用、レイアウト調整、2Dスプライト/アニメーション管理、宣言的UI構築

### High-Level GameKit (19ツール) - ゲームシステム構築

**Logic Pillar (12):** Actor, Manager, Health, Combat, Spawner, AI, TriggerZone, Timer, Machinations, SceneFlow, Save, StatusEffect
**UI Pillar (3):** UICommand, UIBinding, Dialogue
**Presentation Pillar (4):** Effect, AnimationSync, VFX, Audio, Feedback

用途: ゲームメカニクス、ターン制御、リソース経済、シーン遷移、インタラクション、HP/ダメージ、戦闘システム、スポーン、タイマー/クールダウン、AI行動、UIデータバインディング、視覚/聴覚フィードバック

## 基本ワークフロー

1. 確認: `unity_scene_crud(operation='inspect', includeHierarchy=True)`
2. 操作: 適切なレイヤーのツールで変更実施
3. 検証: `operation='inspect'` で結果確認

## 🔄 バッチ順次処理 (推奨: 複雑な多段階操作)

### unity_batch_sequential_execute - 順次実行＆リジューム機能

複数のUnity操作を順番に実行し、エラー時に停止して再開できるツール。

**主な機能:**
- 順次実行: 操作を1つずつ順番に実行
- エラー停止: 最初のエラーで停止し、残りの操作を保存
- リジューム: 失敗した操作から再開可能
- 進捗管理: 実行状態の保存と確認

**使用例:**
```python
# 新規実行: 複数のGameObjectを順番に作成
unity_batch_sequential_execute(
    operations=[
        {'tool': 'unity_gameobject_crud', 'arguments': {'operation': 'create', 'name': 'Enemy1', 'parentPath': 'Enemies'}},
        {'tool': 'unity_component_crud', 'arguments': {'operation': 'add', 'gameObjectPath': 'Enemies/Enemy1', 'componentType': 'UnityEngine.Rigidbody2D'}},
        {'tool': 'unity_gameobject_crud', 'arguments': {'operation': 'create', 'name': 'Enemy2', 'parentPath': 'Enemies'}},
        {'tool': 'unity_component_crud', 'arguments': {'operation': 'add', 'gameObjectPath': 'Enemies/Enemy2', 'componentType': 'UnityEngine.Rigidbody2D'}},
    ],
    resume=False,
    stop_on_error=True
)

# リジューム: エラーから再開
unity_batch_sequential_execute(resume=True)
```

**ユースケース:**
- シーンセットアップ: 複数のGameObject作成とコンポーネント追加
- レベル構築: 地形、敵、アイテムの段階的配置
- 設定変更: 複数のプロジェクト設定を順番に更新
- 依存関係: 前の操作の成功が必要な一連の操作

**リソース確認:**
バッチキューの状態は `unity_batch_queue_status` リソースで確認可能

## 🎮 High-Level GameKit Tools (推奨: ゲームシステム構築)

### GameKit Actor - ゲームアクター

```python
# 作成
unity_gamekit_actor(
    operation='create',
    actorId='player_001',
    behaviorProfile='2dPhysics',  # '2dLinear'|'2dTileGrid'|'3dCharacterController'|'3dPhysics'|'3dNavMesh'
    controlMode='directController',  # 'aiAutonomous'|'uiCommand'|'scriptTriggerOnly'
    position={'x': 0, 'y': 0, 'z': 0}
)

# 更新
unity_gamekit_actor(operation='update', actorId='player_001', position={'x': 5, 'y': 0, 'z': 0})

# 検査
unity_gamekit_actor(operation='inspect', actorId='player_001')
```

### GameKit Manager - ゲームマネージャー

```python
# 作成
unity_gamekit_manager(
    operation='create',
    managerId='game_manager',
    managerType='turnBased',  # 'realtime'|'resourcePool'|'eventHub'|'stateManager'
    turnPhases=['PlayerTurn', 'EnemyTurn'],
    persistent=True  # DontDestroyOnLoad
)

# ターン進行
unity_gamekit_manager(operation='update', managerId='game_manager', advancePhase=True)
```

### GameKit Interaction - インタラクション

```python
# 作成
unity_gamekit_interaction(
    operation='create',
    interactionId='door_trigger',
    triggerType='trigger',  # 'collision'|'raycast'|'proximity'|'input'
    actions=[{'type': 'changeScene', 'target': 'Level2'}],
    conditions=[{'type': 'tag', 'value': 'Player'}]
)
```

**アクションタイプ:** spawnPrefab, destroyObject, playSound, sendMessage, changeScene

### GameKit UI Command - UIコマンドパネル

```python
unity_gamekit_ui_command(
    operation='createCommandPanel',
    panelId='CommandPanel',
    canvasPath='Canvas',
    commands=[
        {'name': 'Move', 'commandType': 'move', 'label': '移動', 'moveDirection': {'x': 1, 'y': 0, 'z': 0}},
        {'name': 'Attack', 'commandType': 'action', 'label': '攻撃'},
    ],
    layout='horizontal',  # 'vertical'|'grid'
    targetType='actor',
    targetActorId='player_001'
)
```

### GameKit Machinations - リソース経済システム

Machinations風のリソースフロー管理。再利用可能なScriptableObjectアセットとして定義。

**4つの構成要素:**
1. Resource Pools: リソースプール（HP、MP、Gold等）の初期値/最小値/最大値
2. Resource Flows: 時間経過による自動生成/消費（例: 毎秒MP+1回復）
3. Resource Converters: リソース変換（例: Gold 10 → HP 50）
4. Resource Triggers: 閾値イベント（例: HP≤0で死亡イベント）

```python
# アセット作成
unity_gamekit_machinations(
    operation='create',
    diagramId='player_economy',
    assetPath='Assets/Economy/PlayerEconomy.asset',
    initialResources=[
        {'name': 'health', 'initialAmount': 100, 'minValue': 0, 'maxValue': 100},
        {'name': 'mana', 'initialAmount': 50, 'minValue': 0, 'maxValue': 100}
    ],
    flows=[
        {'flowId': 'manaRegen', 'resourceName': 'mana', 'ratePerSecond': 1.0, 'isSource': True, 'enabledByDefault': True}
    ],
    converters=[
        {'converterId': 'healthPotion', 'fromResource': 'gold', 'toResource': 'health', 'conversionRate': 5.0, 'inputCost': 10}
    ],
    triggers=[
        {'triggerName': 'death', 'resourceName': 'health', 'thresholdType': 'below', 'thresholdValue': 1, 'enabledByDefault': True}
    ]
)

# マネージャーに適用
unity_gamekit_machinations(operation='apply', assetPath='Assets/Economy/PlayerEconomy.asset', managerId='resource_manager', resetExisting=False)
```

### GameKit SceneFlow - シーン遷移管理

```python
# 作成
unity_gamekit_sceneflow(operation='create', flowId='main_flow')

# シーン追加
unity_gamekit_sceneflow(
    operation='addScene',
    flowId='main_flow',
    sceneName='Title',
    scenePath='Assets/Scenes/Title.unity',
    loadMode='single'
)

# 遷移追加
unity_gamekit_sceneflow(
    operation='addTransition',
    flowId='main_flow',
    fromScene='Title',
    toScene='Level1',
    trigger='StartGame'
)

# 遷移実行
unity_gamekit_sceneflow(operation='transition', flowId='main_flow', triggerName='StartGame')
```

### GameKit Health - HP/ダメージシステム

```python
# 作成
unity_gamekit_health(
    operation='create',
    targetPath='Player',
    healthId='player_hp',
    maxHealth=100,
    invincibilityDuration=1.0,
    onDeath='respawn',  # 'destroy'|'disable'|'event'
    respawnDelay=2.0
)

# ダメージ/回復/即死/リスポーン
unity_gamekit_health(operation='applyDamage', healthId='player_hp', amount=25)
unity_gamekit_health(operation='heal', healthId='player_hp', amount=50)
unity_gamekit_health(operation='kill', healthId='player_hp')
unity_gamekit_health(operation='respawn', healthId='player_hp')
```

**UnityEvents:** OnDamage, OnHeal, OnDeath, OnRespawn, OnInvincibilityStart/End

### GameKit Spawner - スポーンシステム

```python
# 作成
unity_gamekit_spawner(
    operation='create',
    targetPath='Spawner',
    spawnerId='enemy_spawner',
    prefabPath='Assets/Prefabs/Enemy.prefab',
    spawnMode='interval',  # 'wave'|'burst'|'manual'
    spawnInterval=3.0,
    maxActive=10,
    autoStart=True
)

# ウェーブ追加
unity_gamekit_spawner(
    operation='addWave',
    spawnerId='enemy_spawner',
    waves=[{'count': 5, 'spawnInterval': 1.0, 'delay': 5.0}]
)

# 操作
unity_gamekit_spawner(operation='start', spawnerId='enemy_spawner')
unity_gamekit_spawner(operation='spawnOne', spawnerId='enemy_spawner')
unity_gamekit_spawner(operation='spawnBurst', spawnerId='enemy_spawner', count=10)
unity_gamekit_spawner(operation='despawnAll', spawnerId='enemy_spawner')
```

### GameKit Timer - タイマー/クールダウン

```python
# タイマー作成
unity_gamekit_timer(
    operation='createTimer',
    targetPath='GameManager',
    timerId='round_timer',
    duration=60.0,
    loop=False,
    autoStart=True
)

# クールダウン作成
unity_gamekit_timer(
    operation='createCooldown',
    targetPath='Player',
    cooldownId='attack_cd',
    cooldownDuration=0.5
)

# 操作
unity_gamekit_timer(operation='startTimer', timerId='round_timer')
unity_gamekit_timer(operation='inspectCooldown', cooldownId='attack_cd')  # → isReady, remainingTime
```

### GameKit AI - AI行動

```python
# 作成
unity_gamekit_ai(
    operation='create',
    targetPath='Enemy',
    aiId='enemy_ai',
    behaviorType='patrolAndChase',  # 'patrol'|'chase'|'flee'
    moveSpeed=3.0,
    detectionRadius=8.0,
    fieldOfView=120,
    patrolMode='pingPong'  # 'loop'|'random'
)

# パトロール地点追加
unity_gamekit_ai(operation='addPatrolPoint', aiId='enemy_ai', position={'x': 0, 'y': 0, 'z': 5})

# ターゲット設定
unity_gamekit_ai(operation='setTarget', aiId='enemy_ai', targetPath='Player')
```

### GameKit Collectible - 収集アイテム

```python
unity_gamekit_collectible(
    operation='create',
    name='GoldCoin',
    collectibleId='coin_001',
    collectibleType='coin',  # 'health'|'mana'|'powerup'|'key'|'ammo'|'experience'|'custom'
    value=10,
    collectionBehavior='destroy',  # 'disable'|'respawn'
    respawnDelay=30.0
)
```

### GameKit Projectile - 弾丸/ミサイル

```python
unity_gamekit_projectile(
    operation='create',
    name='Bullet',
    projectileId='bullet_001',
    movementType='rigidbody',  # 'transform'|'rigidbody2d'
    speed=20.0,
    damage=10,
    lifetime=5.0,
    isHoming=False,
    canBounce=False
)
```

### GameKit Waypoint - パス追従

```python
unity_gamekit_waypoint(
    operation='create',
    name='Platform',
    waypointId='platform_001',
    pathMode='pingpong',  # 'once'|'loop'
    moveSpeed=3.0,
    autoStart=True,
    waypointPositions=[
        {'x': 0, 'y': 0, 'z': 0},
        {'x': 0, 'y': 5, 'z': 0}
    ]
)
```

### GameKit TriggerZone - トリガーゾーン

```python
unity_gamekit_trigger_zone(
    operation='create',
    name='Checkpoint',
    zoneId='checkpoint_001',
    zoneType='checkpoint',  # 'damagezone'|'healzone'|'teleport'|'speedboost'|'slowdown'|'killzone'|'safezone'|'trigger'
    is2D=True,
    colliderShape='box',
    colliderSize={'x': 2, 'y': 3, 'z': 2}
)
```

## 🎨 3-Pillar Architecture Tools (v2.7.0)

### GameKit UI Binding - 宣言的UIデータバインディング

ゲーム状態（Health, Economy, Timer等）をUI要素に自動バインド。

```python
# HPバーをプレイヤーHealthにバインド
unity_gamekit_ui_binding(
    operation='create',
    targetPath='Canvas/HUD/HPBar',
    bindingId='player_hp_bar',
    sourceType='health',      # 'health'|'economy'|'timer'|'custom'
    sourceId='player_health',
    format='percent',         # 'raw'|'percent'|'ratio'|'formatted'
    smoothTransition=True,
    transitionSpeed=5.0
)

# 経済リソース（Gold）をテキストにバインド
unity_gamekit_ui_binding(
    operation='create',
    targetPath='Canvas/HUD/GoldText',
    bindingId='gold_counter',
    sourceType='economy',
    sourceId='game_manager',
    targetProperty='gold',
    format='raw'
)
```

**自動検出UIコンポーネント:** Slider, Image (fill), Text, TMP_Text

### GameKit Combat - 統合ダメージ計算システム

```python
# 近接攻撃作成
unity_gamekit_combat(
    operation='create',
    targetPath='Player',
    combatId='player_melee',
    attackType='melee',       # 'melee'|'ranged'|'aoe'|'projectile'
    baseDamage=25,
    critChance=0.1,
    critMultiplier=2.0,
    hitbox={'type': 'sphere', 'radius': 1.5},  # 'sphere'|'box'|'capsule'|'cone'
    targetTags=['Enemy'],
    attackCooldown=0.5,
    onHitEffectId='slash_effect',
    onCritEffectId='crit_effect'
)

# ターゲットタグ追加/削除
unity_gamekit_combat(operation='addTargetTag', combatId='player_melee', tag='Boss')
unity_gamekit_combat(operation='resetCooldown', combatId='player_melee')
```

**UnityEvents:** OnHit, OnCrit, OnMiss, OnKill

### GameKit Feedback - ゲームフィール演出

ヒットストップ、画面シェイク、フラッシュ等のゲームフィール演出。

```python
# フィードバック作成
unity_gamekit_feedback(
    operation='create',
    targetPath='FeedbackManager',
    feedbackId='hit_feedback',
    playOnEnable=False,
    globalIntensityMultiplier=1.0,
    components=[
        {'type': 'hitstop', 'duration': 0.05, 'hitstopTimeScale': 0.0},
        {'type': 'screenShake', 'intensity': 0.3, 'duration': 0.15, 'shakeFrequency': 25},
        {'type': 'flash', 'color': {'r': 1, 'g': 1, 'b': 1, 'a': 0.5}, 'duration': 0.05}
    ]
)

# コンポーネント追加
unity_gamekit_feedback(
    operation='addComponent',
    feedbackId='hit_feedback',
    component={'type': 'scale', 'scaleAmount': {'x': 1.2, 'y': 1.2, 'z': 1.2}, 'duration': 0.1}
)
```

**コンポーネントタイプ:** hitstop, screenShake, flash, colorFlash, scale, position, rotation, sound, particle, haptic

### GameKit VFX - ビジュアルエフェクト

パーティクルシステムのラッパー（プーリング、ライフサイクル管理）。

```python
# VFX作成
unity_gamekit_vfx(
    operation='create',
    targetPath='Effects/Explosion',
    vfxId='explosion_vfx',
    particlePrefabPath='Assets/Prefabs/Explosion.prefab',
    autoPlay=False,
    loop=False,
    usePooling=True,
    poolSize=10,
    durationMultiplier=1.0,
    sizeMultiplier=1.0,
    emissionMultiplier=1.0
)

# 乗数設定
unity_gamekit_vfx(operation='setMultipliers', vfxId='explosion_vfx', duration=1.5, size=2.0, emission=1.0)
```

### GameKit Audio - オーディオ再生

フェード制御付きオーディオラッパー。

```python
# オーディオ作成
unity_gamekit_audio(
    operation='create',
    targetPath='AudioManager/BGM',
    audioId='bgm_main',
    audioType='music',        # 'sfx'|'music'|'ambient'|'voice'|'ui'
    audioClipPath='Assets/Audio/BGM/Main.mp3',
    playOnEnable=True,
    loop=True,
    volume=0.8,
    fadeInDuration=2.0,
    fadeOutDuration=1.0
)

# 操作
unity_gamekit_audio(operation='setVolume', audioId='bgm_main', volume=0.5)
unity_gamekit_audio(operation='setClip', audioId='bgm_main', audioClipPath='Assets/Audio/BGM/Battle.mp3')
```

**オーディオタイプ:** sfx, music, ambient, voice, ui

## ⚡ Mid-Level Batch Tools (推奨: バッチ操作)

### Transform Batch - 配置とリネーム

```python
# 円形配置
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=['Obj1', 'Obj2'], radius=5.0, startAngle=0)

# 直線配置
unity_transform_batch(operation='arrangeLine', gameObjectPaths=[...], startPosition={'x': 0, 'y': 0, 'z': 0}, endPosition={'x': 10, 'y': 0, 'z': 0})

# 連番リネーム
unity_transform_batch(operation='renameSequential', gameObjectPaths=[...], baseName='Enemy', startIndex=1, padding=3)
```

### RectTransform Batch - UIレイアウト

```python
# アンカー設定
unity_rectTransform_batch(operation='setAnchors', gameObjectPaths=[...], anchorMin={'x': 0, 'y': 0}, anchorMax={'x': 1, 'y': 1})

# 親に整列
unity_rectTransform_batch(operation='alignToParent', gameObjectPaths=[...], preset='topLeft')  # 'middleCenter'|'bottomRight'等

# 分配
unity_rectTransform_batch(operation='distributeHorizontal', gameObjectPaths=[...], spacing=10)
```

### Physics Bundle - 物理プリセット

```python
# 2Dプリセット
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'], preset='character')
# プリセット: 'dynamic'|'kinematic'|'static'|'character'|'platformer'|'topDown'|'vehicle'|'projectile'

# 3Dプリセット
unity_physics_bundle(operation='applyPreset3D', gameObjectPaths=['Player'], preset='character')
```

### Camera Rig - カメラ設定

```python
unity_camera_rig(
    operation='createRig',
    rigType='follow',  # 'orbit'|'splitScreen'|'fixed'|'dolly'
    rigName='MainCamera',
    targetPath='Player',
    offset={'x': 0, 'y': 5, 'z': -10},
    followSpeed=5.0
)
```

### UI Foundation - UI基礎要素

```python
# Canvas
unity_ui_foundation(operation='createCanvas', name='GameUI', renderMode='screenSpaceOverlay')

# Panel
unity_ui_foundation(operation='createPanel', name='Panel', parentPath='GameUI', anchorPreset='middleCenter', width=400, height=300)

# Button
unity_ui_foundation(operation='createButton', name='Button', parentPath='GameUI', text='Click', width=200, height=60)

# LayoutGroup追加
unity_ui_foundation(operation='addLayoutGroup', targetPath='GameUI/Panel', layoutType='Vertical', spacing=10, padding={'left': 10, 'right': 10, 'top': 10, 'bottom': 10})
```

⚠️ `layoutType`: 'Horizontal'|'Vertical'|'Grid' ※`targetPath`必須（`parentPath`ではない）

### UI Hierarchy - 宣言的UI構築

```python
# JSON定義から複雑なUI階層を一括作成
unity_ui_hierarchy(
    operation='create',
    parentPath='Canvas',
    hierarchy={
        'type': 'panel',
        'name': 'Menu',
        'children': [
            {'type': 'text', 'name': 'Title', 'text': 'Game Menu', 'fontSize': 32},
            {'type': 'button', 'name': 'StartBtn', 'text': 'Start Game'},
            {'type': 'button', 'name': 'OptionsBtn', 'text': 'Options'},
        ],
        'layout': 'Vertical',
        'spacing': 20
    }
)

# 表示/非表示
unity_ui_hierarchy(operation='show', gameObjectPath='Canvas/Menu')
unity_ui_hierarchy(operation='hide', gameObjectPath='Canvas/Menu')
```

### UI State - UI状態管理

```python
# 状態定義
unity_ui_state(operation='defineState', stateName='hidden', rootPath='Canvas/Dialog', elements=[{'path': '', 'active': False, 'alpha': 0}])

# 状態適用
unity_ui_state(operation='applyState', stateName='hidden', rootPath='Canvas/Dialog')

# 状態グループ
unity_ui_state(operation='createStateGroup', groupName='MenuStates', states=['main', 'options', 'credits'])
```

### UI Navigation - UIナビゲーション設定

```python
# 自動設定
unity_ui_navigation(operation='autoSetup', rootPath='Canvas/Menu', direction='vertical', wrapAround=True)

# 明示的設定
unity_ui_navigation(operation='setExplicit', gameObjectPath='Canvas/Button1', up='Canvas/Button0', down='Canvas/Button2')
```

### Audio Source Bundle - オーディオ設定

```python
unity_audio_source_bundle(
    operation='createAudioSource',
    gameObjectPath='BGMPlayer',
    preset='music',  # 'sfx'|'ambient'|'voice'|'ui'
    audioClipPath='Assets/Audio/bgm.mp3',
    volume=1.0,
    loop=True
)
```

### Character Controller Bundle - 3Dキャラクター制御

```python
# プリセット適用
unity_character_controller_bundle(operation='applyPreset', gameObjectPath='Player', preset='fps')
# プリセット: 'fps'|'tps'|'platformer'|'child'|'large'|'narrow'|'custom'

# カスタム設定
unity_character_controller_bundle(operation='update', gameObjectPath='Player', radius=0.5, height=2.0, slopeLimit=45.0, stepOffset=0.3)
```

## 🔧 Low-Level CRUD Tools (詳細制御)

### Scene & GameObject

```python
# 作成
unity_gameobject_crud(operation='create', name='Player', parentPath='Characters')

# 更新
unity_gameobject_crud(operation='update', gameObjectPath='Player', tag='Player', layer='Player', active=True)

# 複数検索
unity_gameobject_crud(operation='findMultiple', pattern='Enemy*', maxResults=100)
```

### Component

```python
# 追加
unity_component_crud(operation='add', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'gravityScale': 0})

# 更新
unity_component_crud(operation='update', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'mass': 2.0})

# バッチ追加
unity_component_crud(operation='addMultiple', pattern='Enemy*', componentType='UnityEngine.BoxCollider2D', maxResults=100)
```

#### Unity Object参照 (propertyChanges内)

```python
# アセット参照
{'$ref': 'Assets/Materials/Player.mat'}

# シーンオブジェクト（非アクティブも検索可能）
{'$ref': 'Canvas/Panel/Button'}
```

### Asset & Script

```python
# テキストファイル作成
unity_asset_crud(operation='create', assetPath='Assets/Data/config.json', content='{"version": 1}')

# インポーター設定
unity_asset_crud(operation='updateImporter', assetPath='Assets/Textures/sprite.png', propertyChanges={'textureType': 'Sprite'})
```

⚠️ C#スクリプト作成/更新後は自動コンパイル待機（60秒タイムアウト）

### ScriptableObject

```python
# 作成
unity_scriptableObject_crud(operation='create', typeName='MyGame.GameConfig', assetPath='Assets/Data/GameConfig.asset', properties={'version': 1, 'maxPlayers': 4})

# 検査
unity_scriptableObject_crud(operation='inspect', assetPath='Assets/Data/GameConfig.asset', includeProperties=True)

# 型検索
unity_scriptableObject_crud(operation='findByType', typeName='MyGame.GameConfig', searchPath='Assets/Data', maxResults=100)
```

### Prefab

```python
# 作成
unity_prefab_crud(operation='create', gameObjectPath='Player', prefabPath='Assets/Prefabs/Player.prefab')

# インスタンス化
unity_prefab_crud(operation='instantiate', prefabPath='Assets/Prefabs/Enemy.prefab', parentPath='Enemies', position={'x': 0, 'y': 0, 'z': 5})
```

### Vector Sprite (プロトタイプ用)

```python
# プリミティブ生成
unity_vector_sprite_convert(operation='primitiveToSprite', primitiveType='circle', color={'r': 1, 'g': 0, 'b': 0, 'a': 1}, outputPath='Assets/Sprites/RedCircle.png', width=256, height=256)

# 単色スプライト
unity_vector_sprite_convert(operation='createColorSprite', width=64, height=64, color={'r': 0, 'g': 1, 'b': 0, 'a': 1}, outputPath='Assets/Sprites/Green.png')
```

### Project Settings

```python
# 読み取り
unity_projectSettings_crud(operation='read', category='physics2d', property='gravity')

# 書き込み
unity_projectSettings_crud(operation='write', category='physics2d', property='gravity', value={'x': 0, 'y': -9.81})

# タグ/レイヤー追加
unity_projectSettings_crud(operation='write', category='tagsLayers', property='addTag', value='Enemy')
unity_projectSettings_crud(operation='write', category='tagsLayers', property='addLayer', value='Projectile')
```

### Build Settings

```python
# ビルドシーン一覧
unity_projectSettings_crud(operation='listBuildScenes')

# シーン追加
unity_projectSettings_crud(operation='addSceneToBuild', scenePath='Assets/Scenes/Level1.unity', index=0, enabled=True)

# シーン順序変更
unity_projectSettings_crud(operation='reorderBuildScenes', fromIndex=0, toIndex=2)
```

## ⚡ Performance & Best Practices

### ツール選択ガイド

1. **ゲームシステム構築** → High-Level GameKit
2. **複数オブジェクト処理** → Mid-Level Batch
3. **詳細な個別制御** → Low-Level CRUD

### 高速化テクニック

1. `includeProperties=False`: コンポーネント存在確認のみ（10倍高速）
2. `propertyFilter`: 必要なプロパティのみ取得
3. `maxResults`: 大量操作時のタイムアウト防止（デフォルト1000）
4. `stopOnError=False`: バッチ処理でエラー時も続行

### バッチ操作（推奨）

```python
# ❌ 避ける: ループ内で個別ツール呼び出し

# ✅ 推奨1: Mid-Level Batchツールまたは *Multiple 操作
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=[...])
unity_component_crud(operation='addMultiple', pattern='Enemy*', componentType='...')

# ✅ 推奨2: 複雑な多段階処理には unity_batch_sequential_execute
unity_batch_sequential_execute(operations=[...], resume=False)
```

## 🔌 Utility Tools

### unity_ping - 接続確認

```python
unity_ping()  # ブリッジ接続状態を確認
```

### unity_compilation_await - コンパイル待機

```python
unity_compilation_await(operation='await', timeoutSeconds=60)
```

通常はスクリプト操作後に自動で呼び出されるが、明示的に待機したい場合に使用

## 🔧 Troubleshooting

### 接続エラー

1. `unity_ping` で接続確認
2. Unity Editor: Tools > MCP Assistant が起動しているか確認
3. ポート7077が使用可能か確認

### コンパイルエラー

- C#スクリプト作成/更新後は自動コンパイル待機
- レスポンスの `compilationWait` 情報を確認
- Unity Editorのコンソールログでエラー詳細を確認

### タイムアウト

1. `maxResults` を減らす（デフォルト1000 → 100以下）
2. `includeProperties=False` で高速化
3. `stopOnError=False` でバッチ操作続行
4. Mid-Level Batchツールを使用（個別呼び出しより高速）

### バッチ順次処理エラー

- エラー発生時は `stopped_at_index` で失敗位置を確認
- 問題を修正後、`resume=True` で再開
- `unity_batch_queue_status` リソースで進捗確認
- 完全にリセットする場合は新規実行（`resume=False`）

## 📋 Quick Reference

### 全44ツール一覧

**High-Level GameKit - Logic (12):**
unity_gamekit_actor, unity_gamekit_manager, unity_gamekit_health, unity_gamekit_combat, unity_gamekit_spawner, unity_gamekit_ai, unity_gamekit_trigger_zone, unity_gamekit_timer, unity_gamekit_machinations, unity_gamekit_sceneflow, unity_gamekit_save, unity_gamekit_status_effect

**High-Level GameKit - UI (4):**
unity_gamekit_ui_command, unity_gamekit_ui_binding, unity_gamekit_dialogue, unity_gamekit_inventory

**High-Level GameKit - Presentation (5):**
unity_gamekit_effect, unity_gamekit_animation_sync, unity_gamekit_vfx, unity_gamekit_audio, unity_gamekit_feedback

**High-Level GameKit - Legacy (3):**
unity_gamekit_interaction, unity_gamekit_collectible, unity_gamekit_projectile, unity_gamekit_waypoint

**Mid-Level Batch (14):**
unity_transform_batch, unity_rectTransform_batch, unity_physics_bundle, unity_camera_rig, unity_ui_foundation, unity_ui_hierarchy, unity_ui_state, unity_ui_navigation, unity_audio_source_bundle, unity_input_profile, unity_character_controller_bundle, unity_tilemap_bundle, unity_sprite2d_bundle, unity_animation2d_bundle

**Low-Level CRUD (8):**
unity_scene_crud, unity_gameobject_crud, unity_component_crud, unity_asset_crud, unity_scriptableObject_crud, unity_prefab_crud, unity_vector_sprite_convert, unity_projectSettings_crud

**Batch Operations (1):**
unity_batch_sequential_execute

**Utility (2):**
unity_ping, unity_compilation_await

### GameObject識別方法

- `gameObjectPath`: 階層パス（例: `'Canvas/Panel/Button'`）
- `gameObjectGlobalObjectId`: GlobalObjectId文字列（シーン再読み込み後も安定）

### よくあるコンポーネントプロパティ

- **Transform**: position, rotation, localScale
- **RectTransform**: anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot
- **Rigidbody**: mass, drag, useGravity, constraints
- **Rigidbody2D**: mass, linearDamping, angularDamping, gravityScale, constraints
- **Camera**: fieldOfView, clearFlags, backgroundColor

### 命名規則

全ツール名は `unity_*` 形式（アンダースコア区切り）

---

Unity-AI-Forge v{VERSION} - 50+ Tools, 120+ Operations, 3-Pillar Architecture (UI: Binding/List/Slot/Selection, Logic: Combat/Health/AI, Presentation: VFX/Audio/Feedback) + Reorganized Handler Structure (LowLevel/MidLevel/Utility) + UI-First Design + Batch Processing + Machinations Economics + Physics2D
