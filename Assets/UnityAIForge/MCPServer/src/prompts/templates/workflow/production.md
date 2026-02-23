# 本番制作 ワークフローガイド (Unity-AI-Forge v{VERSION})

プロトタイプ検証済みのゲームを、品質・安定性・パフォーマンスを備えた製品レベルに引き上げるためのワークフローガイドです。

---

## 概要

本番制作フェーズでは、プロトタイプで動作確認したゲームループに対して、アセットパイプライン・演出・品質ゲートを順序立てて適用します。Unity-AI-ForgeのPresentation Pillarツール（effect, feedback, audio, vfx, animation_sync）とLogic Pillarの検証ツール（validate_integrity, scene_relationship_graph）を活用して、品質を段階的に高めていきます。

**本番制作の原則:**
- プロトタイプのゲームループは変えない。演出・品質を上乗せする
- アセットパイプライン（インポート設定）を最初に固める
- 演出は Presentation Pillar ツールで生成コードとして追加
- 各マイルストーンで必ず validate_integrity を実行する
- Prefabワークフローで変更を一元管理する
- scene_relationship_graph で参照の健全性を定期確認する

---

## ワークフロー概要

```
アセットインポート設定 → マテリアル・シェーダー整備
→ アニメーション組み込み → 演出追加 (Effect/VFX/Feedback)
→ オーディオ追加 → UI本番化 → 品質ゲート確認
→ Prefab化・最適化 → リリースビルド準備
```

---

## 推奨手順

1. **アセットパイプライン整備** - スプライト・テクスチャのインポート設定を固める
2. **マテリアルセットアップ** - シェーダー選択・プロパティ設定
3. **アニメーション組み込み** - AnimatorController・クリップ・遷移設定
4. **Presentation Pillar適用** - Effect/VFX/Feedback/Audioを生成コードで追加
5. **UI本番化** - プロトタイプのデバッグUIを製品UIに置き換え
6. **UIバインディング設定** - データバインディングで動的表示
7. **イベント接続** - UnityEventのwiring
8. **品質ゲート** - validate_integrity + scene_relationship_graph
9. **Prefab化・整理** - 本番Prefab階層を整備
10. **パフォーマンス確認** - console_log + playmode でプロファイリング

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: アセットパイプライン整備

```python
# スプライトのインポート設定
unity_asset_crud(operation='updateImporter',
    assetPath='Assets/Textures/Player.png',
    propertyChanges={
        'textureType': 'Sprite',
        'spritePixelsPerUnit': 32,
        'filterMode': 1,        # Bilinear
        'textureCompression': 1 # Normal
    })

# スプライトシートのスライス（正しい操作名・パラメータ名）
unity_sprite2d_bundle(operation='sliceSpriteSheet',
    texturePath='Assets/Textures/PlayerSheet.png',
    sliceMode='grid',
    cellSizeX=32, cellSizeY=32)

# タイルマップ用テクスチャ
unity_asset_crud(operation='updateImporter',
    assetPath='Assets/Textures/Tileset.png',
    propertyChanges={'textureType': 'Sprite', 'spriteMode': 2})  # Multiple

unity_tilemap_bundle(operation='createTilemap',
    tilemapName='Ground',
    tilesetPath='Assets/Textures/Tileset.png')
```

### Step 2: マテリアルセットアップ

```python
# キャラクター用マテリアル (URP Lit) - savePath を使用
unity_material_bundle(operation='create',
    savePath='Assets/Materials/Player.mat',
    shader='Universal Render Pipeline/Lit')

# プロパティ変更は update 操作を使用（setProperty は存在しない）
unity_material_bundle(operation='update',
    materialPath='Assets/Materials/Player.mat',
    properties={
        '_Smoothness': 0.3,
        '_Metallic': 0.0
    })

# テクスチャの設定
unity_material_bundle(operation='setTexture',
    materialPath='Assets/Materials/Player.mat',
    propertyName='_BaseMap',
    texturePath='Assets/Textures/Player.png')

# エミッシブ（発光）マテリアル
unity_material_bundle(operation='create',
    savePath='Assets/Materials/GlowEffect.mat',
    shader='Standard')

unity_material_bundle(operation='applyPreset',
    materialPath='Assets/Materials/GlowEffect.mat',
    preset='emissive')

# スプライトマテリアル（2Dキャラ用）
unity_material_bundle(operation='create',
    savePath='Assets/Materials/Sprite.mat',
    shader='Sprites/Default')
```

### Step 3: アニメーション組み込み

```python
# 2Dアニメーション: AnimatorController作成
unity_animation2d_bundle(operation='createController',
    controllerPath='Assets/Animations/Player.controller')

# クリップをスプライトから作成
unity_animation2d_bundle(operation='createClipFromSprites',
    clipPath='Assets/Animations/Idle.anim',
    sprites=['Assets/Textures/PlayerSheet_0', 'Assets/Textures/PlayerSheet_1'],
    frameRate=8, loop=True)

unity_animation2d_bundle(operation='createClipFromSprites',
    clipPath='Assets/Animations/Run.anim',
    sprites=['Assets/Textures/PlayerSheet_4', 'Assets/Textures/PlayerSheet_5',
             'Assets/Textures/PlayerSheet_6', 'Assets/Textures/PlayerSheet_7'],
    frameRate=12, loop=True)

# AnimatorController にステートと遷移を追加
unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player.controller',
    stateName='Idle', clipPath='Assets/Animations/Idle.anim')

unity_animation2d_bundle(operation='addState',
    controllerPath='Assets/Animations/Player.controller',
    stateName='Run', clipPath='Assets/Animations/Run.anim')

unity_animation2d_bundle(operation='addParameter',
    controllerPath='Assets/Animations/Player.controller',
    parameterName='Speed', parameterType='Float')

unity_animation2d_bundle(operation='addTransition',
    controllerPath='Assets/Animations/Player.controller',
    fromState='Idle', toState='Run',
    conditions=[{'parameter': 'Speed', 'mode': 'Greater', 'threshold': 0.1}])

# Animator を GameObject にセットアップ
unity_animation2d_bundle(operation='setupAnimator',
    gameObjectPath='Player',
    controllerPath='Assets/Animations/Player.controller')

# アニメーション同期 (Presentation Pillar)
unity_gamekit_animation_sync(operation='create',
    targetPath='Player',
    syncId='player_anim',
    syncSource='rigidbody2d',
    animatorPath='Player')

unity_gamekit_animation_sync(operation='addSyncRule',
    syncId='player_anim',
    parameterName='Speed',
    sourceField='velocity.magnitude')

unity_gamekit_animation_sync(operation='addTriggerRule',
    syncId='player_anim',
    triggerName='Attack',
    eventSource='input',
    eventType='attack')

# コンパイル待機
unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 4: 演出追加 - VFX・エフェクト

```python
# パーティクルエフェクト
unity_particle_bundle(operation='create',
    gameObjectPath='FX/HitEffect',
    preset='hit')

unity_particle_bundle(operation='create',
    gameObjectPath='FX/ExplosionEffect',
    preset='explosion')

# VFXをGameKitで管理 (プーリング対応)
unity_gamekit_vfx(operation='create',
    targetPath='FX/HitVFX',
    vfxId='hit_vfx',
    particlePrefabPath='Assets/Prefabs/FX/HitEffect.prefab',
    usePooling=True,
    poolSize=20)

# 複合エフェクト (爆発 = パーティクル + カメラシェイク + スクリーンフラッシュ)
unity_gamekit_effect(operation='create',
    targetPath='FX/Explosion',
    effectId='explosion',
    components=[
        {'type': 'particle',    'prefabPath': 'Assets/Prefabs/FX/ExplosionEffect.prefab'},
        {'type': 'cameraShake', 'intensity': 0.6, 'duration': 0.4},
        {'type': 'screenFlash', 'color': {'r': 1.0, 'g': 0.8, 'b': 0.0, 'a': 0.5}, 'duration': 0.1},
        {'type': 'timeScale',   'scale': 0.1, 'duration': 0.05}
    ])

# Effect Manager を作成
unity_gamekit_effect(operation='createManager', targetPath='EffectManager')

# コンパイル待機
unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 5: フィードバック追加

```python
# ヒット時フィードバック
unity_gamekit_feedback(operation='create',
    targetPath='FeedbackManager',
    feedbackId='player_hit',
    components=[
        {'type': 'hitstop',     'duration': 0.06},
        {'type': 'screenShake', 'intensity': 0.25, 'duration': 0.2},
        {'type': 'colorFlash',  'color': {'r': 1.0, 'g': 0.0, 'b': 0.0, 'a': 0.8}, 'duration': 0.15},
        {'type': 'sound',       'clipPath': 'Assets/Audio/SFX/HitPlayer.wav'}
    ])

# コイン取得フィードバック
unity_gamekit_feedback(operation='create',
    targetPath='FeedbackManager',
    feedbackId='coin_collect',
    components=[
        {'type': 'scale',    'targetScale': {'x': 1.3, 'y': 1.3, 'z': 1.3}, 'duration': 0.1},
        {'type': 'sound',    'clipPath': 'Assets/Audio/SFX/Coin.wav'},
        {'type': 'particle', 'prefabPath': 'Assets/Prefabs/FX/SparkleEffect.prefab'}
    ])

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 6: オーディオ追加

```python
# BGM (GameKit Audio - コード生成で管理機能付き)
unity_gamekit_audio(operation='create',
    targetPath='Audio/BGM',
    audioId='game_bgm',
    audioType='music',
    audioClipPath='Assets/Audio/BGM/GameTheme.mp3',
    loop=True,
    volume=0.7)

# 環境音
unity_gamekit_audio(operation='create',
    targetPath='Audio/Ambient',
    audioId='ambient',
    audioType='ambient',
    audioClipPath='Assets/Audio/Ambient/Forest.mp3',
    loop=True,
    volume=0.4)

# SE 用 AudioSource (シンプルなコンポーネント設定)
unity_audio_source_bundle(operation='createAudioSource',
    gameObjectPath='Audio/SFXSource',
    preset='sfx')

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 7: UI本番化

```python
# デバッグUIを削除して本番UIを構築
unity_gameobject_crud(operation='delete', gameObjectPath='DebugUI')

# 本番Canvas
unity_ui_foundation(operation='createCanvas', name='GameUI',
    renderMode='ScreenSpaceOverlay',
    referenceResolution={'x': 1920, 'y': 1080})

# HUD構造を宣言的に構築
unity_ui_hierarchy(operation='create', parentPath='GameUI',
    hierarchy={
        'type': 'panel', 'name': 'HUD',
        'children': [
            {'type': 'image', 'name': 'HPBar',     'sprite': {'$ref': 'Assets/UI/HPBar.png'}},
            {'type': 'text',  'name': 'ScoreText',  'text': '0', 'fontSize': 28},
            {'type': 'image', 'name': 'MiniMap',    'anchors': 'topRight'}
        ]
    })

# HPバーにデータバインディング
unity_gamekit_ui_binding(operation='create',
    targetPath='GameUI/HUD/HPBar',
    bindingId='hp_bar',
    sourceType='health',
    sourceId='player_hp',
    format='ratio')

# スコアにバインディング
unity_gamekit_ui_binding(operation='create',
    targetPath='GameUI/HUD/ScoreText',
    bindingId='score_text',
    sourceType='custom',
    sourceId='score',
    format='formatted',
    formatString='{0:D6}')

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 8: イベント接続

```python
# ボタンのイベントを接続
unity_event_wiring(operation='wireMultiple',
    wirings=[
        {
            'source': {'gameObject': 'GameUI/Menu/StartButton', 'component': 'Button', 'event': 'onClick'},
            'target': {'gameObject': 'GameManager', 'method': 'StartGame'}
        },
        {
            'source': {'gameObject': 'GameUI/Menu/QuitButton', 'component': 'Button', 'event': 'onClick'},
            'target': {'gameObject': 'GameManager', 'method': 'QuitGame'}
        }
    ])

# ボタンにナビゲーション設定
unity_ui_navigation(operation='autoSetup',
    rootPath='GameUI/Menu',
    direction='vertical')
```

### Step 9: 品質ゲート

```python
# 整合性チェック（全項目）
unity_validate_integrity(operation='all')

# Missing Scriptを自動除去
unity_validate_integrity(operation='removeMissingScripts')

# シーン遷移・参照の全体確認
unity_scene_relationship_graph(operation='analyzeAll')
unity_scene_relationship_graph(operation='validateBuildSettings')

# 孤立オブジェクト検出
unity_scene_reference_graph(operation='findOrphans')

# コンパイルエラー確認
unity_console_log(operation='getCompilationErrors')
unity_console_log(operation='getErrors')

# プレイモードで最終動作確認
unity_playmode_control(operation='play')
unity_console_log(operation='getErrors')
unity_playmode_control(operation='stop')
```

### Step 10: Prefab整理

```python
# 本番Prefab作成
unity_prefab_crud(operation='create',
    gameObjectPath='Player',
    prefabPath='Assets/Prefabs/Characters/Player.prefab')

unity_prefab_crud(operation='create',
    gameObjectPath='Enemy',
    prefabPath='Assets/Prefabs/Characters/Enemy.prefab')

# Prefab変更をシーンに適用
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Player')
```

---

## チェックリスト

### アセットパイプライン
- [ ] スプライト/テクスチャのインポート設定を確定した
- [ ] sprite2d_bundle(sliceSpriteSheet) でスプライトシートをスライスした
- [ ] material_bundle(create) で savePath を指定してマテリアルを作成した

### アニメーション
- [ ] animation2d_bundle(createController) でControllerを作成した
- [ ] animation2d_bundle(createClipFromSprites) でクリップを作成した
- [ ] animation2d_bundle(setupAnimator) でGameObjectにセットアップした
- [ ] gamekit_animation_sync でRigidbody速度と同期させた

### 演出
- [ ] gamekit_vfx でVFXプーリング管理を設定した
- [ ] gamekit_effect で複合エフェクトを設定した
- [ ] gamekit_feedback でヒットフィードバックを設定した
- [ ] gamekit_audio でBGM・SE・環境音を設定した

### UI
- [ ] デバッグUIを本番UIに置き換えた
- [ ] gamekit_ui_binding でHP・スコアを動的表示にした
- [ ] event_wiring でボタンにイベントを接続した
- [ ] ui_navigation でキーボード/ゲームパッド操作を設定した

### 品質ゲート
- [ ] validate_integrity(all) でエラーなし確認
- [ ] scene_relationship_graph(analyzeAll) で参照正常確認
- [ ] console_log(getErrors) でエラーなし確認
- [ ] playmode_control で動作最終確認

### Prefab
- [ ] 全キャラクター・アイテムをPrefab化した
- [ ] prefab_crud(applyOverrides) で変更を適用した

---

## 注意点・落とし穴

**GameKit生成コードはcompilation_awaitが必要**
effect, feedback, vfx, audio, animation_sync はコード生成を行うため、生成後に必ず compilation_await を実行してください。

**material_bundle(create) は savePath で保存先を指定**
materialPath ではなく savePath パラメータを使用します。プロパティ変更には update 操作を使用してください（setProperty は存在しません）。

**sprite2d_bundle のスライス操作名は sliceSpriteSheet**
sliceSheet ではありません。パラメータは sliceMode（sliceType ではない）、cellSizeX/cellSizeY（cellWidth/cellHeight ではない）です。

**animation2d_bundle に create 操作はない**
setupAnimator, createController, createClipFromSprites を個別に使用してください。

**audio_source_bundle の操作名は createAudioSource**
create ではなく createAudioSource です。setMixerGroup 操作は存在しません。

**Presentation Pillarツールは重複作成しない**
同じ effectId/feedbackId/vfxId/audioId で再実行するとエラーになります。

**prefab_crud の適用操作は applyOverrides**
apply ではなく applyOverrides を使用してください。同様に revert ではなく revertOverrides です。

**validate_integrityはプレイモード中には実行しない**
プレイモード停止後に実行してください。

---

## 関連ツール一覧

| ツール | 本番制作での用途 |
|--------|----------------|
| `unity_asset_crud` | updateImporter でインポート設定 |
| `unity_sprite2d_bundle` | sliceSpriteSheet でスプライトシートスライス |
| `unity_tilemap_bundle` | createTilemap でタイルマップ構築 |
| `unity_material_bundle` | create (savePath) / update / setTexture / applyPreset |
| `unity_animation2d_bundle` | setupAnimator / createController / createClipFromSprites |
| `unity_gamekit_animation_sync` | アニメーション同期 (Presentation Pillar) |
| `unity_gamekit_effect` | create / createManager で複合エフェクト管理 |
| `unity_gamekit_feedback` | ヒット感・フィードバック |
| `unity_gamekit_vfx` | VFXプーリング管理 |
| `unity_gamekit_audio` | オーディオ管理 |
| `unity_audio_source_bundle` | createAudioSource でシンプルなAudioSource設定 |
| `unity_gamekit_ui_binding` | データバインディング |
| `unity_ui_foundation` | 本番Canvas・UI要素作成 |
| `unity_ui_hierarchy` | 宣言的UI構築 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_event_wiring` | wire / wireMultiple でUnityEvent接続 |
| `unity_particle_bundle` | パーティクルプリセット作成 |
| `unity_validate_integrity` | all / removeMissingScripts で品質ゲート |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings |
| `unity_scene_reference_graph` | findOrphans で孤立オブジェクト確認 |
| `unity_prefab_crud` | create / applyOverrides で本番Prefab管理 |
| `unity_playmode_control` | 最終動作確認 |
| `unity_console_log` | getCompilationErrors / getErrors でモニタリング |
| `unity_compilation_await` | コード生成後のコンパイル待機 |
