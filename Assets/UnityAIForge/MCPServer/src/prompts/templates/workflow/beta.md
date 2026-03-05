# ベータ ワークフローガイド (Unity-AI-Forge v{VERSION})

演出・ビジュアル・サウンドの本格統合。アルファで完成したゲームロジックに、実アセット・VFX・Audio・Feedback・アニメーションを組み込み、製品レベルの品質に引き上げるためのガイドです。

---

## 概要

ベータフェーズの目的は「見た目と感触を製品レベルにする」ことです。アルファで確立したゲームロジック・データ構造・イベント接続はそのまま維持しつつ、プリミティブを実アセットに置き換え、Mid-Level ツール（animation, material, light, particle 等）で演出を追加します。ロジック層は安定しているため、演出の追加がゲームプレイに影響しない安全な状態で作業できます。

**ベータフェーズの原則:**
- アルファのゲームロジック・イベント接続は変えない。演出・品質を上乗せする
- アセットパイプライン（インポート設定）を最初に固める
- 演出は Mid-Level ツール（animation, material, light, particle）で追加
- 各マイルストーンで必ず validate_integrity を実行する
- scene_relationship_graph で参照の健全性を定期確認する

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → アルファ → [ベータ] → リリース
```

**前提**: アルファでゲームロジック完成済み（`game_workflow_guide(phase='alpha')`）。ScriptableObject・状態管理・イベント接続・UIバインディングが動作している状態。

---

## ワークフロー概要

```
アセットインポート設定 → マテリアル・シェーダー整備
→ アニメーション組み込み → パーティクル演出追加
→ UI本番化 → 品質ゲート確認
```

---

## 推奨手順

1. **アセットパイプライン整備** - スプライト・テクスチャのインポート設定を固める
2. **マテリアルセットアップ** - シェーダー選択・プロパティ設定
3. **アニメーション組み込み** - AnimatorController・クリップ・遷移設定
4. **パーティクル演出追加** - パーティクルエフェクト
5. **UI本番化** - プロトタイプUIを製品品質に引き上げ
6. **品質ゲート** - validate_integrity + scene_relationship_graph
7. **Prefab更新** - 演出付きPrefabの更新

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
```

### Step 4: VFX・エフェクト追加

```python
# パーティクルエフェクト
unity_particle_bundle(operation='create',
    gameObjectPath='FX/HitEffect',
    preset='hit')

unity_particle_bundle(operation='create',
    gameObjectPath='FX/ExplosionEffect',
    preset='explosion')
```

### Step 5: オーディオ追加

```python
# BGM (AudioSource + ループ)
unity_component_crud(operation='add',
    gameObjectPath='Audio/BGM',
    componentType='AudioSource',
    propertyChanges={
        'clip': {'$ref': 'Assets/Audio/BGM/GameTheme.mp3'},
        'loop': True,
        'volume': 0.7,
        'playOnAwake': True
    })

# 環境音
unity_component_crud(operation='add',
    gameObjectPath='Audio/Ambient',
    componentType='AudioSource',
    propertyChanges={
        'clip': {'$ref': 'Assets/Audio/Ambient/Forest.mp3'},
        'loop': True,
        'volume': 0.4,
        'playOnAwake': True
    })
```

### Step 6: UI本番化

プロトタイプ・アルファで構築したUI構造をベースに、ビジュアルを製品品質に引き上げます。バインディング・イベント接続はアルファで済んでいるため、見た目の改善に集中できます。

```python
# Canvas の referenceResolution を本番設定に更新
unity_component_crud(operation='update',
    gameObjectPath='GameUI',
    componentType='UnityEngine.UI.CanvasScaler',
    propertyChanges={
        'uiScaleMode': 1,
        'referenceResolution': {'x': 1920, 'y': 1080}
    })

# HUDのビジュアル改善（テキストをイメージ付きに差し替え等）
# 既存構造を活用しつつ子要素を追加
unity_ui_foundation(operation='createImage', name='HPBarBG', parentPath='GameUI/HUD')
unity_ui_foundation(operation='createImage', name='HPBarFill', parentPath='GameUI/HUD/HPBarBG',
    sprite={'$ref': 'Assets/UI/HPBarFill.png'})

# ライティング設定
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')
```

### Step 9: 品質ゲート

詳細なテスト・検証手法は `game_workflow_guide(phase='testing')` の PDCA サイクルを参照してください。

```python
# 整合性チェック（全項目）
unity_validate_integrity(operation='all')

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

### Step 10: Prefab更新

```python
# 演出付きPrefabの更新
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Player')
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Enemy')

# FX系のPrefab化
unity_prefab_crud(operation='create',
    gameObjectPath='FX/HitEffect',
    prefabPath='Assets/Prefabs/FX/HitEffect.prefab')
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

### 演出
- [ ] particle_bundle でパーティクルエフェクトを作成した
- [ ] component_crud で AudioSource を設定した（BGM・SE・環境音）
- [ ] light_bundle でライティングを設定した

### UI
- [ ] Canvas の referenceResolution を本番設定にした
- [ ] ビジュアル素材（スプライト・フォント）を適用した
- [ ] アルファで設定したバインディング・イベント接続が正常動作することを確認した

### 品質ゲート
- [ ] validate_integrity(all) でエラーなし確認
- [ ] scene_relationship_graph(analyzeAll) で参照正常確認
- [ ] console_log(getErrors) でエラーなし確認
- [ ] playmode_control で動作最終確認

### Prefab
- [ ] 全キャラクター・アイテムのPrefabを演出付きで更新した
- [ ] prefab_crud(applyOverrides) で変更を適用した

---

## 注意点・落とし穴

**material_bundle(create) は savePath で保存先を指定**
materialPath ではなく savePath パラメータを使用します。プロパティ変更には update 操作を使用してください（setProperty は存在しません）。

**sprite2d_bundle のスライス操作名は sliceSpriteSheet**
sliceSheet ではありません。パラメータは sliceMode（sliceType ではない）、cellSizeX/cellSizeY（cellWidth/cellHeight ではない）です。

**animation2d_bundle に create 操作はない**
setupAnimator, createController, createClipFromSprites を個別に使用してください。

**audio_source_bundle の操作名は createAudioSource**
create ではなく createAudioSource です。setMixerGroup 操作は存在しません。

**演出追加でロジックを壊さない**
ベータフェーズでは演出レイヤーのみ追加します。アルファで確立したイベント接続・バインディング・状態管理を変更しないでください。

**validate_integrityはプレイモード中には実行しない**
プレイモード停止後に実行してください。

---

## 次のフェーズへ

ベータで演出・品質が整ったら、リリースフェーズで最終検証を行います:

1. **リリース** (`game_workflow_guide(phase='release')`) - 最終検証・ビルド準備
   - シーン遷移グラフの最終確認
   - Build Settings の最終化
   - 包括的リリースチェックリスト

---

## 関連ツール一覧

| ツール | ベータでの用途 |
|--------|--------------|
| `unity_asset_crud` | updateImporter でインポート設定 |
| `unity_sprite2d_bundle` | sliceSpriteSheet でスプライトシートスライス |
| `unity_tilemap_bundle` | createTilemap でタイルマップ構築 |
| `unity_material_bundle` | create (savePath) / update / setTexture / applyPreset |
| `unity_animation2d_bundle` | setupAnimator / createController / createClipFromSprites |
| `unity_component_crud` | AudioSource 追加・設定 |
| `unity_light_bundle` | ライティング設定 |
| `unity_particle_bundle` | パーティクルプリセット作成 |
| `unity_validate_integrity` | all / typeCheck で品質ゲート |
| `unity_scene_relationship_graph` | analyzeAll / validateBuildSettings |
| `unity_scene_reference_graph` | findOrphans で孤立オブジェクト確認 |
| `unity_prefab_crud` | applyOverrides でPrefab更新 |
| `unity_playmode_control` | 最終動作確認 |
| `unity_console_log` | getCompilationErrors / getErrors でモニタリング |
| `unity_compilation_await` | コード生成後のコンパイル待機 |
