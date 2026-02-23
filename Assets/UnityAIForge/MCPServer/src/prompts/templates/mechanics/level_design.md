# レベルデザイン実装ガイド (Unity-AI-Forge {VERSION})

## 概要

レベルデザインはゲーム空間の構造・配置・流れを設計する工程です。
タイルマップによるマップ構築、スポーンポイントの配置、カメラゾーンの設定、
収集アイテムの散在、トリガーゾーンによるイベント起動など、
ゲームプレイを形作る多くの要素が含まれます。

Unity-AI-Forge では `unity_tilemap_bundle` でタイルマップ構築、
`unity_transform_batch` で大量オブジェクトの一括配置、`unity_camera_rig` でカメラ設定、
`unity_asset_crud` でギミック・トリガーのカスタムスクリプトを作成できます。

---

## 設計パターン

### レベル構成の 5 要素

```
1. 地形 (Terrain/Tilemap)
   +-- プレイヤーが移動する物理空間

2. 配置物 (Props/Decoration)
   +-- 雰囲気と目印を作るオブジェクト

3. ギミック (Gimmick/Hazard)
   +-- 動く床、トゲ、押しボタンなど

4. 収集物 (Collectible)
   +-- コイン、スター、アイテムなど

5. イベントトリガー (Trigger)
   +-- カットシーン起動、ドア開閉、チェックポイント
```

### フロー設計の原則

```
Start -> Tutorial Area -> Challenge Zone 1 -> Rest Area -> Challenge Zone 2 -> Boss / Goal
         (安全・学習)      (適度な難易度)     (リソース補充)  (高難易度)          (クライマックス)
```

### タイルマップ構成（2D）

```
Layers:
  Background    (z: -2) -- 空・海などの背景
  BackDecoration(z: -1) -- 後景装飾（遠い木等）
  Terrain       (z:  0) -- 当たり判定あり地面
  Foreground    (z:  1) -- プレイヤーの前に表示される装飾
```

---

## 推奨フォルダ構造

```
Assets/
  Scenes/
    Levels/
      Level01_Grassland.unity
      Level02_Cave.unity
  Scripts/
    Level/
      LevelManager.cs          # レベル全体管理
      CheckpointManager.cs     # チェックポイント管理
      TriggerZone.cs           # 汎用トリガーゾーン
      MovingPlatform.cs        # 動く床
      Collectible.cs           # 収集アイテム基底
  Tilemaps/
    Grassland/
      Tileset_Grassland.asset
  Prefabs/
    Level/
      Checkpoint.prefab
      SpawnPoint.prefab
      CoinPickup.prefab
  Data/
    Levels/
      Level01_Config.asset     # レベル設定 (ScriptableObject)
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: タイルマップの構築

```python
# タイルマップ基盤を作成
unity_tilemap_bundle(
    operation="createTilemap",
    name="Level01_Terrain",
    cellSize={"x": 1.0, "y": 1.0}
)

# タイルを配置
unity_tilemap_bundle(
    operation="fillArea",
    targetPath="Level01_Terrain",
    tilePath="Assets/Tilemaps/Grassland/GroundTile.asset",
    startPosition={"x": 0, "y": 0, "z": 0},
    endPosition={"x": 30, "y": 0, "z": 0}
)

# コライダーを追加
unity_tilemap_bundle(
    operation="addCollider",
    targetPath="Level01_Terrain"
)
```

### Step 2: スポーンポイントの配置

```python
# プレイヤースポーンポイント
unity_gameobject_crud(
    operation="create",
    name="PlayerSpawnPoint",
    tag="SpawnPoint"
)

# 敵スポーンポイントを複数作成
unity_gameobject_crud(operation="create", name="EnemySpawn_01", parentPath="SpawnPoints")
unity_gameobject_crud(operation="create", name="EnemySpawn_02", parentPath="SpawnPoints")
unity_gameobject_crud(operation="create", name="EnemySpawn_03", parentPath="SpawnPoints")

# transform_batch でスポーンポイントを直線上に整列
unity_transform_batch(
    operation="arrangeLine",
    gameObjectPaths=[
        "SpawnPoints/EnemySpawn_01",
        "SpawnPoints/EnemySpawn_02",
        "SpawnPoints/EnemySpawn_03"
    ],
    startPosition={"x": 10, "y": 1, "z": 0},
    endPosition={"x": 25, "y": 1, "z": 0}
)
```

### Step 3: チェックポイントの設置

```python
# チェックポイントスクリプトを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/CheckpointManager.cs",
    content="""using UnityEngine;
public class CheckpointManager : MonoBehaviour {
    public static CheckpointManager Instance { get; private set; }
    public Vector3 LastCheckpoint { get; private set; }
    void Awake() { Instance = this; LastCheckpoint = transform.position; }
    public void SetCheckpoint(Vector3 position) { LastCheckpoint = position; }
}"""
)

unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/Checkpoint.cs",
    content="""using UnityEngine;
public class Checkpoint : MonoBehaviour {
    private bool activated;
    void OnTriggerEnter2D(Collider2D other) {
        if (!activated && other.CompareTag(\"Player\")) {
            activated = true;
            CheckpointManager.Instance.SetCheckpoint(transform.position);
        }
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# チェックポイントを配置
unity_gameobject_crud(
    operation="create",
    name="Checkpoint_01",
    parentPath="Checkpoints",
    components=[
        {"type": "Checkpoint"},
        {"type": "UnityEngine.BoxCollider2D",
         "properties": {"isTrigger": true, "size": {"x": 2, "y": 3}}}
    ]
)

# チェックポイント到達エフェクト
unity_gamekit_effect(
    operation="create",
    effectId="checkpoint_effect",
    components=[
        {"type": "particle", "duration": 1.0},
        {"type": "sound",    "volume": 0.6}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 4: カメラの設定

```python
# プレイヤー追従カメラ
unity_camera_rig(
    operation="createRig",
    targetPath="MainCamera",
    preset="follow"
)

# ボス戦エリアの固定カメラ用ゾーンオブジェクト
unity_gameobject_crud(
    operation="create",
    name="BossArena_CameraZone",
    components=[
        {"type": "UnityEngine.BoxCollider2D",
         "properties": {"isTrigger": true, "size": {"x": 20, "y": 14}}}
    ]
)
```

### Step 5: 収集アイテムの配置

```python
# 収集アイテムスクリプトを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/Collectible.cs",
    content="""using UnityEngine;
public class Collectible : MonoBehaviour {
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private string collectibleType = \"coin\";
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag(\"Player\")) {
            // スコア加算処理
            Destroy(gameObject);
        }
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# コインを複数配置
unity_gameobject_crud(operation="create", name="Coin_01", parentPath="Collectibles",
    components=[{"type": "Collectible"},
        {"type": "UnityEngine.CircleCollider2D", "properties": {"isTrigger": true, "radius": 0.5}}])
unity_gameobject_crud(operation="create", name="Coin_02", parentPath="Collectibles",
    components=[{"type": "Collectible"},
        {"type": "UnityEngine.CircleCollider2D", "properties": {"isTrigger": true, "radius": 0.5}}])
unity_gameobject_crud(operation="create", name="Coin_03", parentPath="Collectibles",
    components=[{"type": "Collectible"},
        {"type": "UnityEngine.CircleCollider2D", "properties": {"isTrigger": true, "radius": 0.5}}])

# transform_batch でコインを直線配置
unity_transform_batch(
    operation="arrangeLine",
    gameObjectPaths=[
        "Collectibles/Coin_01", "Collectibles/Coin_02", "Collectibles/Coin_03"
    ],
    startPosition={"x": 5, "y": 2, "z": 0},
    endPosition={"x": 15, "y": 2, "z": 0}
)

# ピックアップ時のオーディオ
unity_gamekit_audio(
    operation="create",
    targetPath="Collectibles",
    audioId="coin_pickup_sfx",
    audioType="sfx",
    volume=0.5,
    pitchVariation=0.1
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 6: トリガーゾーンの設置

```python
# ゴールトリガースクリプト
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/GoalTrigger.cs",
    content="""using UnityEngine;
using UnityEngine.SceneManagement;
public class GoalTrigger : MonoBehaviour {
    [SerializeField] private string nextSceneName;
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag(\"Player\")) {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# ゴールゾーンを配置
unity_gameobject_crud(
    operation="create",
    name="GoalTrigger",
    components=[
        {"type": "GoalTrigger"},
        {"type": "UnityEngine.BoxCollider2D",
         "properties": {"isTrigger": true, "size": {"x": 2, "y": 3}}}
    ]
)

# シーンをビルド設定に追加
unity_projectSettings_crud(
    operation="addSceneToBuild",
    scenePath="Assets/Scenes/Levels/Level01_Grassland.unity"
)

unity_projectSettings_crud(
    operation="addSceneToBuild",
    scenePath="Assets/Scenes/Levels/Level02_Cave.unity"
)
```

### Step 7: 動く床ギミック

```python
# 動く床スクリプト
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/MovingPlatform.cs",
    content="""using UnityEngine;
public class MovingPlatform : MonoBehaviour {
    [SerializeField] private Transform pointA, pointB;
    [SerializeField] private float speed = 2f;
    private bool goingToB = true;
    void Update() {
        var target = goingToB ? pointB.position : pointA.position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.01f) goingToB = !goingToB;
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 8: レイアウト確認

```python
# シーン全体の参照関係チェック
unity_scene_relationship_graph(
    operation="analyzeAll",
    format="summary"
)

# 整合性チェック
unity_validate_integrity(operation="all")

# ビルド設定の確認
unity_scene_relationship_graph(operation="validateBuildSettings")
```

---

## よくあるパターン

### パターン 1: 装飾の一括配置

```python
# 木オブジェクトを作成
unity_gameobject_crud(operation="create", name="Tree_01", parentPath="Decoration")
unity_gameobject_crud(operation="create", name="Tree_02", parentPath="Decoration")
unity_gameobject_crud(operation="create", name="Tree_03", parentPath="Decoration")

# 円形に配置
unity_transform_batch(
    operation="arrangeCircle",
    gameObjectPaths=["Decoration/Tree_01", "Decoration/Tree_02", "Decoration/Tree_03"],
    center={"x": 15, "y": 0, "z": 0},
    radius=5.0,
    plane="XY"
)
```

### パターン 2: BGM ゾーン

```python
# 環境 BGM を配置
unity_gamekit_audio(
    operation="create",
    targetPath="Level01_BGM",
    audioId="field_bgm",
    audioType="music",
    audioClipPath="Assets/Audio/BGM/Field.mp3",
    playOnEnable=true,
    loop=true,
    volume=0.6,
    fadeInDuration=1.0
)
```

### パターン 3: レベル設定の ScriptableObject

```python
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Level/LevelConfig.cs",
    content="""using UnityEngine;
[CreateAssetMenu(fileName = \"NewLevel\", menuName = \"Game/LevelConfig\")]
public class LevelConfig : ScriptableObject {
    public string levelName;
    public int targetScore;
    public float timeLimit;
    public string nextLevelScene;
}"""
)
```

---

## 注意点・落とし穴

1. **タイルマップのコライダー設定**
   Terrain レイヤーに `Composite Collider 2D` + `Tilemap Collider 2D (Use by Composite)` を
   設定すること。バラバラのコライダーは処理が重くなる。

2. **スポーン位置の地面チェック**
   スポーンポイントが地面より上に配置されているか常に確認すること。
   地面と重なるとプレイヤーが地面にめり込む。

3. **コード生成後のコンパイル待ち**
   `unity_asset_crud` でスクリプト作成後、`unity_gamekit_effect` 等のコード生成後は、
   必ず `unity_compilation_await` を呼ぶこと。

4. **カメラ境界のズレ**
   Camera Confiner の境界はタイルマップのサイズより若干内側に設定しないと、
   端でカメラが境界外を映してしまう。

5. **シーン遷移時のオブジェクト管理**
   `DontDestroyOnLoad` 対象オブジェクトを明示的に管理し、重複生成を防ぐ。

6. **パフォーマンス: 収集アイテムの最適化**
   大量のコインを配置する場合は Object Pool パターンを使用し、
   非アクティブなコインを再利用すること。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_tilemap_bundle` | タイルマップの作成・タイル配置 |
| `unity_gameobject_crud` | スポーンポイント・トリガーゾーン配置 |
| `unity_asset_crud` | ギミック・トリガーの C# スクリプト作成 |
| `unity_transform_batch` | 大量オブジェクトの一括整列・配置 |
| `unity_physics_bundle` | ギミックの物理設定 |
| `unity_camera_rig` | フォロー・固定カメラ設定 |
| `unity_prefab_crud` | レベルオブジェクトのプレハブ化 |
| `unity_gamekit_effect` | チェックポイント・収集エフェクト |
| `unity_gamekit_audio` | 環境 BGM・SE・ゾーン別音楽 |
| `unity_sprite2d_bundle` | 2D スプライトの管理・設定 |
| `unity_projectSettings_crud` | ビルドシーンの追加・レイヤー設定 |
| `unity_scriptableObject_crud` | レベル設定 ScriptableObject |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_scene_relationship_graph` | レベル間遷移・ビルド設定の確認 |
| `unity_validate_integrity` | 配置後の整合性チェック |
