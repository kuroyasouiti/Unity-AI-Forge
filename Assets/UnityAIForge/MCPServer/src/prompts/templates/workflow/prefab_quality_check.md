# Prefab品質チェック ワークフローガイド (Unity-AI-Forge v{VERSION})

プレハブの整合性・レイヤー/タグ設定・コンポーネント構成を検証するガイドです。
大量のプレハブを扱うプロジェクトで、設定漏れや不整合を早期発見します。

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → アルファ → ベータ → リリース
                                        ↑            ↑          ↑
                              [Prefab品質チェック: プレハブ作成後に随時実施]
```

---

## 概要

ゲーム開発ではプレハブ（敵、弾、パワーアップ、UI要素等）を大量に作成します。
各プレハブのタグ・レイヤー・コライダー設定が正しくないと、
衝突判定の不具合やパフォーマンス問題に繋がります。

このガイドでは、MCPツールを使ったプレハブ品質検証と一括修正のワークフローを提供します。

---

## 推奨手順

### Step 1: プレハブ一覧の取得

```python
# Assets/Prefabs 以下の全プレハブを列挙
unity_asset_crud(operation='findMultiple',
    pattern='Assets/Prefabs/**/*.prefab',
    maxResults=200)
```

### Step 2: プレハブの個別検査

```python
# 各プレハブの構成を確認
unity_prefab_crud(operation='inspect',
    prefabPath='Assets/Prefabs/Enemies/BasicEnemy.prefab')

# コンポーネント詳細も確認する場合
# → instantiate → component_crud(inspect, componentType='*') → delete
# または editAsset で直接確認
```

### Step 3: タグ・レイヤー設定の一括適用

```python
# editMultiple で敵プレハブのタグ・レイヤーを一括設定
unity_prefab_crud(operation='editMultiple',
    prefabPaths=[
        'Assets/Prefabs/Enemies/BasicEnemy.prefab',
        'Assets/Prefabs/Enemies/FastEnemy.prefab',
        'Assets/Prefabs/Enemies/TankEnemy.prefab',
        'Assets/Prefabs/Enemies/SniperEnemy.prefab',
    ],
    tag='Enemy', layer='Enemy')

# 弾プレハブの一括設定
unity_prefab_crud(operation='editMultiple',
    prefabPaths=[
        'Assets/Prefabs/Bullets/PlayerBullet.prefab',
        'Assets/Prefabs/Bullets/PlayerBulletWide.prefab',
    ],
    tag='PlayerBullet', layer='PlayerBullet',
    componentChanges=[
        {'componentType': 'CircleCollider2D',
         'propertyChanges': {'isTrigger': True}}
    ])
```

### Step 4: コリジョンマトリクスの一括設定

```python
# setCollisionMatrixBatch で不要な衝突を一括除外
unity_physics_bundle(operation='setCollisionMatrixBatch', is2D=True, pairs=[
    {'layerA': 'Player', 'layerB': 'PlayerBullet', 'ignore': True},
    {'layerA': 'Enemy', 'layerB': 'EnemyBullet', 'ignore': True},
    {'layerA': 'PlayerBullet', 'layerB': 'EnemyBullet', 'ignore': True},
    {'layerA': 'PlayerBullet', 'layerB': 'PlayerBullet', 'ignore': True},
    {'layerA': 'EnemyBullet', 'layerB': 'EnemyBullet', 'ignore': True},
    {'layerA': 'Pickup', 'layerB': 'EnemyBullet', 'ignore': True},
    {'layerA': 'Pickup', 'layerB': 'PlayerBullet', 'ignore': True},
    {'layerA': 'Pickup', 'layerB': 'Enemy', 'ignore': True},
])
```

### Step 5: 整合性検証

```python
# シーン整合性チェック（Missing Script, null参照, 壊れたEvent/Prefab）
unity_validate_integrity(operation='all')

# Prefab 個別検証
unity_validate_integrity(operation='checkPrefab',
    prefabPath='Assets/Prefabs/Enemies/BasicEnemy.prefab')

# 未使用アセットの検出
unity_scene_dependency(operation='findUnusedAssets',
    searchPath='Assets/Prefabs')
```

### Step 6: ランタイム検証

```python
# スナップショット取得 → プレイ → diff で新規エラーのみ確認
unity_console_log(operation='snapshot')
unity_playmode_control(operation='play')

# 数秒待ってから状態キャプチャ
unity_playmode_control(operation='captureState',
    targets=['Player', 'GameManager'],
    includeSerializedFields=True,
    includeConsole=True)

unity_playmode_control(operation='stop')

# スナップショット後の新規エラーのみ表示
unity_console_log(operation='diff', severity=['error', 'warning'])
```

---

## チェックリスト

### タグ・レイヤー
- [ ] 全敵プレハブに Enemy タグ・レイヤーが設定されている
- [ ] 全弾プレハブに PlayerBullet / EnemyBullet タグ・レイヤーが設定されている
- [ ] パワーアップに Pickup タグが設定されている
- [ ] 未使用のタグ・レイヤーがないか確認

### コライダー
- [ ] 弾・パワーアップの Collider は isTrigger=true に設定
- [ ] 高速移動オブジェクト（弾等）は Collision Detection = Continuous
- [ ] コライダーサイズが適切（大きすぎ/小さすぎないか）

### 衝突マトリクス
- [ ] 自弾同士は衝突しない (PlayerBullet-PlayerBullet ignore)
- [ ] 敵弾同士は衝突しない (EnemyBullet-EnemyBullet ignore)
- [ ] 自弾と敵弾は衝突しない (PlayerBullet-EnemyBullet ignore)
- [ ] 自弾と自機は衝突しない (Player-PlayerBullet ignore)
- [ ] 敵弾と敵は衝突しない (Enemy-EnemyBullet ignore)

### 参照整合性
- [ ] validate_integrity(all) でエラーなし
- [ ] プレイモードでNullReferenceExceptionなし
- [ ] 孤立オブジェクトなし (scene_reference_graph findOrphans)

---

## よくある問題と対処

### タグ未設定のプレハブ
**症状**: OnTriggerEnter2D の CompareTag が false を返す
**対処**: `editMultiple` で一括設定

### レイヤー未設定のプレハブ
**症状**: 衝突マトリクスが効かず、不要な衝突が発生
**対処**: `editMultiple` で layer を設定し、`setCollisionMatrixBatch` で除外

### isTrigger 未設定
**症状**: 弾が物理的に跳ね返る/押し合う
**対処**: `editAsset` で componentChanges を使い isTrigger=true に設定

---

## 関連ツール一覧

| ツール | 用途 |
|--------|------|
| `unity_prefab_crud` | editAsset / editMultiple / inspect / create |
| `unity_physics_bundle` | setCollisionMatrixBatch / setCollisionMatrix / inspect |
| `unity_validate_integrity` | all / checkPrefab |
| `unity_scene_dependency` | findUnusedAssets |
| `unity_asset_crud` | findMultiple (プレハブ列挙) |
| `unity_console_log` | snapshot / diff (ランタイムエラー検出) |
| `unity_playmode_control` | captureState (includeSerializedFields) |
| `unity_projectSettings_crud` | tagsLayers (タグ・レイヤー追加) |
